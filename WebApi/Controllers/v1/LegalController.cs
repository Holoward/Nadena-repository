using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/[controller]")]
public class LegalController : ControllerBase
{
private const string RefundPolicyText = @"NADENA REFUND AND DISPUTE POLICY

1. ELIGIBILITY
Data Clients may request a refund within 7 days of purchase if the data quality is demonstrably poor. Examples of poor quality include: missing expected fields, excessive anonymization rendering data unusable, or dataset does not match the advertised description.

2. PROCESS
To request a refund, contact support@nadena.com with your purchase ID and a detailed explanation of the quality issue. Our admin team will review the request within 3 business days.

3. ASSESSMENT
Refund requests are assessed by admin review. The admin may request additional information or evidence of the quality issue. Refunds are granted at the sole discretion of Nadena administration.

4. REFUND METHOD
Approved refunds are processed back to the original payment method via Stripe. Processing time is typically 5-10 business days depending on your financial institution.

5. EXCLUSIONS
Refunds are not available for: (a) requests made after 7 days from purchase, (b) the client's change of mind, (c) data that matches the advertised description but does not meet the client's expectations.

6. DISPUTES
For disputes not resolved through the refund process, parties agree to mediation before pursuing legal action. Contact legal@nadena.com for dispute resolution.";

    // GET: api/v1/Legal/refund-policy
    [HttpGet("refund-policy")]
    [AllowAnonymous]
    public IActionResult GetRefundPolicy()
    {
        return Content(RefundPolicyText, "text/plain");
    }

    // GET: api/v1/Legal/privacy-policy
    [HttpGet("privacy-policy")]
    [AllowAnonymous]
    public IActionResult GetPrivacyPolicy()
    {
        var privacyPolicy = @"NADENA PRIVACY POLICY

Last Updated: March 2026

1. INTRODUCTION

Nadena ('we', 'us', or 'our') operates the Nadena platform. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our platform.

Please read this privacy policy carefully. If you do not agree with the terms of this privacy policy, please do not access the platform.

2. DATA WE COLLECT

We collect personal data that you voluntarily provide to us when you register on the platform, express an interest in obtaining information about us or our products and services, when you participate in activities on the platform, or otherwise when you contact us.

This includes:
- Name and contact data (email, payout contact as configured)
- Account data (username, password)
- Usage data (comments, viewing history)
- Payment data (payment records)

3. HOW WE USE YOUR DATA

We use your personal data for the following purposes:
- To provide and maintain our services
- To process your payments
- To comply with legal obligations
- To communicate with you about your account
- To improve our services

4. DATA PROTECTION RIGHTS (GDPR)

Under the General Data Protection Regulation (GDPR), you have the following rights:
- Right to access your personal data
- Right to rectification of your personal data
- Right to erasure ('right to be forgotten')
- Right to restrict processing
- Right to data portability
- Right to object

To exercise any of these rights, please contact us using the information provided below.

5. DATA RETENTION

We will retain your personal data only for as long as is necessary for the purposes set out in this privacy policy.

6. CONTACT US

If you have questions or comments about this policy, you may email us at privacy@nadena.com.";

        return Ok(new { content = privacyPolicy });
    }

    // GET: api/v1/Legal/consent-form
    [HttpGet("consent-form")]
    [AllowAnonymous]
    public IActionResult GetConsentForm()
    {
        var consentText = @"NADENA DATA CONSENT FORM

By consenting to share your YouTube viewing data with Nadena, you agree to the following:

1. DATA SHARED
You are providing access to your Google Takeout watch history, which will be processed to extract anonymized viewing patterns including:
- Total videos watched
- When you watch (time of day, day of week)
- Content categories you prefer
- Viewing session patterns

2. DATA NOT SHARED
We will NOT share:
- Video titles or URLs
- Channel names
- Your email (unless you voluntarily provide it for payment)
- Any personally identifiable information

3. USAGE
Your anonymized data may be licensed to academic researchers and data analysts to study YouTube consumption patterns. You will receive compensation when your data is licensed to approved buyers.

4. REVOCATION
You may request deletion of your data at any time by contacting privacy@nadena.com. This will remove all your data from our systems, but does not affect data already licensed to third parties.

5. COMPENSATION
You will receive a share of revenue when your anonymized data is licensed to verified data buyers. Revenue share is calculated based on data quality and volume.

6. CONSENT
By checking the consent box and clicking 'Share My Data', you confirm that:
- You are the owner of the account associated with this data
- You have read and agree to this consent form
- You understand how your data will be used";

        return Ok(new { content = consentText });
    }

    // GET: api/v1/Legal/terms
    [HttpGet("terms")]
    [AllowAnonymous]
    public IActionResult GetTerms()
    {
        var terms = @"NADENA TERMS OF SERVICE

Last Updated: March 2026

1. ACCEPTANCE OF TERMS

By accessing and using the Nadena platform ('Service'), you accept and agree to be bound by the terms and provision of this agreement.

2. DESCRIPTION OF SERVICE

Nadenais a consent-based data monetization platform that allows volunteers to share their viewing and listening data in exchange for compensation.

3. USER RESPONSIBILITIES

As a user of Nadena, you agree to:
- Provide accurate and complete information
- Maintain the security of your account
- Not share your login credentials
- Comply with all applicable laws and regulations

4. VOLUNTEER CONTRIBUTIONS

Data Contributors may submit data including:
- YouTube comment history
- Spotify listening records
- Netflix viewing history

By submitting data, you confirm that you have the right to share this data and consent to its use as described in our Privacy Policy.

5. PAYMENT TERMS

- Data Contributors will receive payment for validated data contributions
- Payments are recorded in an internal ledger (external disbursement methods may vary)
- Payment amounts are determined by data quality and quantity

6. DATA USAGE

Data submitted to Nadena may be:
- Used to create aggregated, anonymized datasets
- Licensed to approved data clients for research purposes
- Used to improve Nadena services

7. TERMINATION

We reserve the right to terminate your account at any time for violation of these terms.

8. LIMITATION OF LIABILITY

NADENA SHALL NOT BE LIABLE FOR ANY INDIRECT, INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR PUNITIVE DAMAGES.

9. GOVERNING LAW

These Terms shall be governed by the laws of the United Kingdom.

10. CONTACT INFORMATION

For questions about these terms, please contact us at legal@nadena.com.";

        return Ok(new { content = terms });
    }
}
