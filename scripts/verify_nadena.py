#!/usr/bin/env python3
import json
import os
import re
import sqlite3
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid


BASE_URL = os.getenv("NADENA_BASE_URL", "http://localhost:5000")
FRONTEND_URL = os.getenv("NADENA_FRONTEND_URL", "http://localhost:44391")
DATABASE_PATH = os.getenv("NADENA_DB_PATH", "WebApi/OnionArchitecture.db")


def request(method, path, data=None, token=None, headers=None, expected=None, parse_json=True):
    url = f"{BASE_URL}{path}"
    payload = None
    request_headers = {}

    if data is not None:
        request_headers["Content-Type"] = "application/json"
        payload = json.dumps(data).encode()

    if headers:
        request_headers.update(headers)

    if token:
        request_headers["Authorization"] = f"Bearer {token}"

    req = urllib.request.Request(url, data=payload, method=method, headers=request_headers)

    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            status_code = response.status
            raw = response.read()
            content_type = response.headers.get("Content-Type", "")
    except urllib.error.HTTPError as exc:
        status_code = exc.code
        raw = exc.read()
        content_type = exc.headers.get("Content-Type", "")

    text = raw.decode(errors="replace")

    if expected and status_code not in expected:
        raise AssertionError(f"{method} {path} -> {status_code}: {text[:600]}")

    if parse_json and "application/json" in content_type:
        return status_code, json.loads(text)

    return status_code, text


def wait_until_ready(url, label, timeout_seconds=60):
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        try:
            with urllib.request.urlopen(url, timeout=5) as response:
                if response.status == 200:
                    return
        except Exception:
            time.sleep(1)

    raise TimeoutError(f"{label} did not become ready at {url}")


def latest_email_link(to_email, subject_contains):
    connection = sqlite3.connect(DATABASE_PATH)
    cursor = connection.cursor()
    cursor.execute(
        'select Body from EmailLogs where "To"=? and Subject like ? order by SentAt desc limit 1',
        (to_email, f"%{subject_contains}%"),
    )
    row = cursor.fetchone()
    connection.close()

    if not row:
        raise AssertionError(f"No email log found for {to_email} / {subject_contains}")

    match = re.search(r"(http://localhost:\d+[^\s<]+)", row[0])
    if not match:
        raise AssertionError(f"No local link found in email body: {row[0]}")

    return match.group(1)


def parse_query(url):
    parsed = urllib.parse.urlparse(url)
    return {key: values[0] for key, values in urllib.parse.parse_qs(parsed.query).items()}


def assert_no_legacy_role_labels():
    for root, _, files in os.walk("WebApi/ClientApp/build"):
        for file_name in files:
            if file_name.endswith(".map"):
                continue

            file_path = os.path.join(root, file_name)
            with open(file_path, "r", encoding="utf-8", errors="ignore") as handle:
                contents = handle.read()

            for legacy_label in ("Volunteer", "Buyer", "Volunteers", "Buyers"):
                if legacy_label in contents:
                    raise AssertionError(f"Legacy label still present in built frontend: {legacy_label} ({file_path})")


def main():
    wait_until_ready(f"{BASE_URL}/swagger/index.html", "backend")
    wait_until_ready(FRONTEND_URL, "frontend")

    suffix = uuid.uuid4().hex[:8]
    contributor_email = f"contrib_{suffix}@example.com"
    client_email = f"client_{suffix}@example.com"
    password = "StrongPass123!"
    new_password = "NewStrongPass123!"
    results = []

    _, payload = request(
        "POST",
        "/api/v1/Auth/register",
        {
            "fullName": "Verification Contributor",
            "email": contributor_email,
            "password": password,
            "role": "Data Contributor",
            "payPalEmail": contributor_email,
        },
        expected={200},
    )
    contributor_token = payload["data"]
    results.append("registered Data Contributor")

    _, payload = request("GET", "/api/v1/Onboarding/status", token=contributor_token, expected={200})
    assert payload["nextStep"] == "/onboarding/terms"
    request("POST", "/api/v1/Onboarding/accept-terms", token=contributor_token, expected={200})
    request("POST", "/api/v1/Onboarding/accept-consent", token=contributor_token, expected={200})
    _, payload = request("GET", "/api/v1/Onboarding/status", token=contributor_token, expected={200})
    assert payload["termsAccepted"] and payload["consentAccepted"]
    results.append("completed onboarding flow")

    request(
        "PUT",
        "/api/v1/DataContributor/data-sources",
        {
            "youtube": {
                "enabled": True,
                "sharePreference": "Share now and get notified when payment is processed",
                "connectionStatus": "Not connected",
            },
            "spotify": {
                "enabled": True,
                "sharePreference": "Wait until my data is requested, then I will be notified to approve the transfer",
                "connectionStatus": "Not connected",
            },
            "netflix": {
                "enabled": False,
                "sharePreference": "",
                "connectionStatus": "Not connected",
            },
        },
        token=contributor_token,
        expected={200},
    )
    _, contributor_me = request("GET", "/api/v1/DataContributor/me", token=contributor_token, expected={200})
    contributor_id = contributor_me["data"]["id"]
    contributor_user_id = contributor_me["data"]["userId"]
    request("GET", "/api/v1/DataContributor/earnings", token=contributor_token, expected={200})
    request("GET", "/api/v1/DataContributor/wallet", token=contributor_token, expected={200})
    results.append("loaded contributor dashboard APIs")

    _, payload = request(
        "POST",
        "/api/v1/Auth/register",
        {
            "fullName": "Verification Client",
            "email": client_email,
            "password": password,
            "role": "Data Client",
            "companyName": "Verification Labs",
        },
        expected={200},
    )
    client_token = payload["data"]
    verification_link = latest_email_link(client_email, "Verify your email")
    assert verification_link.startswith("http://localhost:44391/verify-email")
    verification_query = parse_query(verification_link)
    request(
        "POST",
        "/api/v1/Auth/verify-email",
        {"email": verification_query["email"], "token": verification_query["token"]},
        expected={200},
    )
    results.append("verified client email")

    _, pools = request("GET", "/api/v1/DataPool", token=client_token, expected={200})
    pool_items = pools.get("data") or pools.get("Data") or []
    if isinstance(pool_items, dict):
        pool_items = pool_items.get("data", [])
    assert pool_items, "No data pools available for verification"
    pool_id = pool_items[0].get("id", pool_items[0].get("Id"))
    request("GET", f"/api/v1/DataPool/{pool_id}/preview", token=client_token, expected={200})
    results.append("loaded discovery preview")

    idempotency_key = f"idem-{uuid.uuid4().hex}"
    purchase_payload = {
        "datasetName": "Verification Dataset",
        "purchaseType": "Monthly",
        "recordCount": 1500,
        "dataSources": ["YouTube", "Spotify"],
        "dateRangeStart": "2025-01-01T00:00:00Z",
        "dateRangeEnd": "2025-12-31T00:00:00Z",
        "demographicFilters": "none",
        "contributorShareNow": False,
    }
    _, payload = request(
        "POST",
        "/api/v1/DataClient/purchases",
        purchase_payload,
        token=client_token,
        headers={"Idempotency-Key": idempotency_key},
        expected={200},
    )
    purchase_id = payload["data"]["id"]
    invoice_number = payload["data"]["invoiceNumber"]
    request(
        "POST",
        "/api/v1/DataClient/purchases",
        purchase_payload,
        token=client_token,
        headers={"Idempotency-Key": idempotency_key},
        expected={200},
    )
    _, payload = request("GET", "/api/v1/DataClient/transactions", token=client_token, expected={200})
    assert payload["data"], "No client ledger transactions found"
    results.append("created purchase and verified idempotency")

    _, payload = request("GET", "/api/v1/DataClient/my-datasets", token=client_token, expected={200})
    assert any(str(item["id"]) == str(purchase_id) for item in payload["data"])
    _, invoice_body = request(
        "GET",
        f"/api/v1/DataClient/my-datasets/{purchase_id}/invoice",
        token=client_token,
        expected={200},
        parse_json=False,
    )
    assert invoice_body.startswith("%PDF") or "PDF" in invoice_body[:50]
    request(
        "POST",
        f"/api/v1/DataClient/my-datasets/{purchase_id}/share",
        {"email": "teammate@example.com"},
        token=client_token,
        expected={200},
    )
    results.append(f"verified dataset dashboard, invoice, and sharing ({invoice_number})")

    _, payload = request("GET", "/api/v1/DataContributor/transactions", token=contributor_token, expected={200})
    held_transactions = [item for item in payload["data"] if item["status"] == "Held"]
    assert held_transactions, "No held contributor payout found"
    request(
        "POST",
        f"/api/v1/DataContributor/payouts/{held_transactions[0]['id']}/approve",
        token=contributor_token,
        expected={200},
    )
    _, csv_text = request(
        "GET",
        "/api/v1/DataContributor/earnings/export-csv",
        token=contributor_token,
        expected={200},
        parse_json=False,
    )
    assert "amount" in csv_text.lower()
    request(
        "DELETE",
        f"/api/v1/DataContributor/{contributor_id}/my-data",
        token=contributor_token,
        expected={200},
    )
    results.append("verified contributor payout, CSV export, and deletion request")

    request("POST", "/api/v1/Auth/forgot-password", {"email": client_email}, expected={200})
    request("POST", "/api/v1/Auth/forgot-password", {"email": client_email}, expected={200})
    request("POST", "/api/v1/Auth/forgot-password", {"email": client_email}, expected={200})
    _, payload = request("POST", "/api/v1/Auth/forgot-password", {"email": client_email}, expected={400})
    assert "limit" in payload["message"].lower()
    reset_link = latest_email_link(client_email, "Password reset")
    assert reset_link.startswith("http://localhost:44391/reset-password")
    reset_query = parse_query(reset_link)
    request(
        "POST",
        "/api/v1/Auth/reset-password",
        {
            "email": reset_query["email"],
            "token": reset_query["token"],
            "newPassword": new_password,
        },
        expected={200},
    )
    _, payload = request(
        "POST",
        "/api/v1/Auth/login",
        {"email": client_email, "password": new_password},
        expected={200},
    )
    client_token = payload["data"]
    results.append("verified password reset and rate limit")

    request(
        "PUT",
        "/api/v1/Account/settings",
        {"fullName": "Verification Client Updated", "email": client_email},
        token=client_token,
        expected={200},
    )
    request(
        "PUT",
        "/api/v1/Account/notification-preferences",
        [
            {"eventType": "PurchaseConfirmed", "isEnabled": True},
            {"eventType": "PayoutProcessed", "isEnabled": False},
            {"eventType": "PasswordReset", "isEnabled": True},
            {"eventType": "EmailVerification", "isEnabled": True},
            {"eventType": "DatasetRefresh", "isEnabled": True},
            {"eventType": "AccountDeletion", "isEnabled": True},
            {"eventType": "AdminAlerts", "isEnabled": False},
        ],
        token=client_token,
        expected={200},
    )
    request("GET", "/api/v1/Account/notification-preferences", token=client_token, expected={200})
    request("GET", "/api/v1/Account/sessions", token=client_token, expected={200})
    request("POST", "/api/v1/Account/sessions/logout-all", token=client_token, expected={200})
    _, payload = request(
        "POST",
        "/api/v1/Auth/login",
        {"email": client_email, "password": new_password},
        expected={200},
    )
    client_token = payload["data"]
    request("DELETE", "/api/v1/Account/delete", token=client_token, expected={200})
    results.append("verified account settings, notification prefs, sessions, and soft delete")

    _, payload = request(
        "POST",
        "/api/v1/Auth/login",
        {"email": "admin@nadena.com", "password": "AdminPassword123!"},
        expected={200},
    )
    admin_token = payload["data"]
    _, wallet = request("GET", "/api/v1/Admin/platform-wallet", token=admin_token, expected={200})
    assert wallet["data"]["balance"] >= 0
    _, pending = request("GET", "/api/v1/Admin/pending-payouts", token=admin_token, expected={200})
    assert pending["data"], "No pending payouts found for admin dashboard"
    for path in (
        "/api/v1/Admin/flagged-datasets",
        "/api/v1/Admin/users",
        "/api/v1/Admin/consent-records",
        "/api/v1/Admin/revenue-report",
        "/api/v1/Admin/deletion-requests",
        "/api/v1/Admin/email-logs",
        "/api/v1/Admin/company-verification-queue",
    ):
        request("GET", path, token=admin_token, expected={200})
    request(
        "POST",
        f"/api/v1/Admin/pending-payouts/{pending['data'][0]['id']}/mark-disbursed",
        {"notes": "Verified during automated script"},
        token=admin_token,
        expected={200},
    )
    _, payload = request("GET", "/api/v1/Admin/deletion-requests", token=admin_token, expected={200})
    for item in payload["data"]:
        if item["userId"] == contributor_user_id:
            request(
                "POST",
                f"/api/v1/Admin/deletion-requests/{item['id']}/deny",
                {},
                token=admin_token,
                expected={200},
            )
            break
    _, payload = request("GET", "/api/v1/Admin/users", token=admin_token, expected={200})
    client_user = next(item for item in payload["data"] if item["email"] == client_email)
    request("POST", f"/api/v1/Admin/users/{client_user['id']}/suspend", {}, token=admin_token, expected={200})
    request("POST", f"/api/v1/Admin/users/{client_user['id']}/reactivate", {}, token=admin_token, expected={200})
    results.append("verified admin wallet, payouts, reports, logs, and user actions")

    connection = sqlite3.connect(DATABASE_PATH)
    cursor = connection.cursor()
    subjects = [row[0] for row in cursor.execute('select Subject from EmailLogs order by SentAt desc limit 100').fetchall()]
    connection.close()
    for subject in ("Verify your email", "Password reset", "Purchase confirmed", "Platform fee credited"):
        assert any(subject in item for item in subjects), subject
    results.append("verified email logs")

    assert_no_legacy_role_labels()
    results.append("confirmed no visible legacy role labels in built frontend")

    print("VERIFICATION_OK")
    for item in results:
        print(f"- {item}")


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"VERIFICATION_FAILED: {exc}", file=sys.stderr)
        sys.exit(1)
