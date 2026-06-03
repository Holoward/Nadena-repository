import React from 'react';

const steps = [
  { label: 'Buyer pays via Stripe', detail: 'WebhookController.cs creates DatasetPurchase, logs revenue split 40/20/40' },
  { label: 'DeliveryEndpoint stored on purchase', detail: 'Buyer provides S3 bucket, SFTP, or HTTPS webhook at checkout' },
  { label: 'Mode pushes notification to contributors', detail: 'Contributor taps → /upload page → authorizes Google OAuth once' },
  { label: 'Contributor taps Create Export on Google Takeout', detail: 'OAuthController.cs stores encrypted refresh token' },
  { label: 'DrivePollingService detects ZIP every 30 minutes', detail: 'TakeoutValidationService.cs validates → anonymizes → DataDeliveryService.cs forwards to DeliveryEndpoint' },
  { label: 'Contributor wallet credited, PayPal payout triggered by admin', detail: 'WalletRepository → PendingBalance → PayVolunteersCommand → PayPal Payouts API' }
];

const s = {
  page: { maxWidth: 600, margin: '0 auto', padding: '32px 16px', fontFamily: 'sans-serif' },
  title: { fontSize: 22, fontWeight: 700, marginBottom: 32, color: '#1a2f4a' },
  step: { display: 'flex', gap: 16, marginBottom: 8 },
  circle: { width: 36, height: 36, minWidth: 36, borderRadius: '50%', background: '#6c47ff', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 14 },
  box: { background: '#f9f8ff', border: '1.5px solid #6c47ff', borderRadius: 10, padding: '12px 16px', flex: 1 },
  label: { fontWeight: 600, fontSize: 15, color: '#1a2f4a', marginBottom: 4 },
  detail: { fontSize: 12, color: '#888', fontFamily: 'monospace' },
  arrow: { textAlign: 'center', color: '#6c47ff', fontSize: 20, marginBottom: 8 }
};

export default function FlowDiagram() {
  return (
    <div style={s.page}>
      <div style={s.title}>Nadena — End to End Flow</div>
      {steps.map((step, i) => (
        <React.Fragment key={i}>
          <div style={s.step}>
            <div style={s.circle}>{i + 1}</div>
            <div style={s.box}>
              <div style={s.label}>{step.label}</div>
              <div style={s.detail}>{step.detail}</div>
            </div>
          </div>
          {i < steps.length - 1 && <div style={s.arrow}>↓</div>}
        </React.Fragment>
      ))}
    </div>
  );
}
