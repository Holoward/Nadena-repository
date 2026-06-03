import React, { useState, useEffect } from 'react';

const STEPS = ['Connect Google', 'Request export', 'Upload file', 'Done'];

const styles = {
  container: { maxWidth: 480, margin: '0 auto', padding: '24px 16px', fontFamily: 'sans-serif' },
  stepper: { display: 'flex', justifyContent: 'space-between', marginBottom: 32 },
  step: (active) => ({
    flex: 1, textAlign: 'center', fontSize: 11,
    fontWeight: active ? 700 : 400,
    color: active ? '#6c47ff' : '#999',
    borderBottom: active ? '2px solid #6c47ff' : '2px solid #eee',
    paddingBottom: 8
  }),
  title: { fontSize: 22, fontWeight: 700, marginBottom: 12 },
  body: { fontSize: 15, color: '#444', lineHeight: 1.6, marginBottom: 24 },
  badge: {
    display: 'inline-block', background: '#e8f5e9', color: '#2e7d32',
    borderRadius: 20, padding: '4px 12px', fontSize: 13, marginBottom: 16
  },
  button: {
    display: 'block', width: '100%', padding: '16px',
    background: '#6c47ff', color: '#fff', border: 'none',
    borderRadius: 10, fontSize: 16, fontWeight: 600,
    cursor: 'pointer', marginBottom: 12, minHeight: 52
  },
  googleButton: {
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    width: '100%', padding: '16px',
    background: '#fff', color: '#333',
    border: '1.5px solid #ddd', borderRadius: 10,
    fontSize: 16, fontWeight: 600,
    cursor: 'pointer', marginBottom: 12, minHeight: 52, gap: 10
  },
  secondaryButton: {
    display: 'block', width: '100%', padding: '16px',
    background: '#f0edff', color: '#6c47ff', border: 'none',
    borderRadius: 10, fontSize: 16, fontWeight: 600,
    cursor: 'pointer', marginBottom: 12, minHeight: 52
  },
  input: {
    display: 'block', width: '100%', padding: '14px',
    border: '1px solid #ddd', borderRadius: 10,
    fontSize: 15, marginBottom: 16, boxSizing: 'border-box'
  },
  fileBox: {
    display: 'block', width: '100%', padding: '20px',
    border: '2px dashed #6c47ff', borderRadius: 10,
    textAlign: 'center', color: '#6c47ff', fontSize: 15,
    cursor: 'pointer', marginBottom: 16, boxSizing: 'border-box',
    background: '#f9f8ff'
  },
  error: { color: '#c0392b', fontSize: 14, marginBottom: 12 },
  success: { fontSize: 16, color: '#27ae60', fontWeight: 600, marginBottom: 8 },
  stat: { fontSize: 14, color: '#555', marginBottom: 4 },
  note: { fontSize: 12, color: '#888', marginTop: 8, lineHeight: 1.5 }
};

export default function TakeoutUpload() {
  const [step, setStep] = useState(0);
  const [file, setFile] = useState(null);
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState(null);
  const [oauthStatus, setOauthStatus] = useState(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const oauth = params.get('oauth');
    if (oauth === 'success') {
      setOauthStatus('success');
      setStep(1);
      window.history.replaceState({}, '', '/upload');
    } else if (oauth) {
      setOauthStatus(oauth);
    }
  }, []);

  const handleConnectGoogle = async () => {
    setLoading(true);
    setError('');
    try {
      const token = localStorage.getItem('token') || sessionStorage.getItem('token');
      const res = await fetch('/api/v1/oauth/google-url', {
        headers: token ? { Authorization: `Bearer ${token}` } : {}
      });
      const data = await res.json();
      if (res.ok && data.url) {
        window.location.href = data.url;
      } else {
        setError('Could not start Google authorization. Please try again.');
      }
    } catch {
      setError('Network error. Please check your connection.');
    } finally {
      setLoading(false);
    }
  };

  const openTakeout = () => {
    window.open(
      'https://takeout.google.com/settings/takeout/custom/youtube,spotify',
      '_blank'
    );
  };

  const handleFileChange = (e) => {
    const selected = e.target.files[0];
    if (selected) setFile(selected);
  };

  const handleUpload = async () => {
    if (!file || !email.trim()) return;
    setLoading(true);
    setError('');
    const token = localStorage.getItem('token') || sessionStorage.getItem('token');
    const formData = new FormData();
    formData.append('zipFile', file);
    formData.append('googleAccountEmail', email.trim());
    try {
      const res = await fetch('/api/v1/takeout/upload', {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData
      });
      const data = await res.json();
      if (res.ok) {
        setResult(data);
        setStep(3);
      } else {
        setError(data.error || 'Upload failed. Please try again.');
      }
    } catch {
      setError('Network error. Please check your connection and try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.stepper}>
        {STEPS.map((label, i) => (
          <div key={label} style={styles.step(step === i)}>{label}</div>
        ))}
      </div>

      {step === 0 && (
        <>
          <div style={styles.title}>Connect your Google account</div>
          <div style={styles.body}>
            Connecting your Google account lets us automatically detect and process
            your Takeout export when it is ready. You will not need to upload anything manually.
          </div>
          {oauthStatus === 'denied' && (
            <div style={styles.error}>
              Google authorization was cancelled. You can still upload manually below.
            </div>
          )}
          <button style={styles.googleButton} onClick={handleConnectGoogle} disabled={loading}>
            <svg width="20" height="20" viewBox="0 0 48 48">
              <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
              <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
              <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
              <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.18 1.48-4.97 2.31-8.16 2.31-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
            </svg>
            {loading ? 'Redirecting...' : 'Continue with Google'}
          </button>
          {error && <div style={styles.error}>{error}</div>}
          <div style={styles.note}>
            We request read-only access to Google Drive to automatically retrieve your
            Takeout export. We never access any other files.
          </div>
          <div style={{ textAlign: 'center', margin: '16px 0', color: '#bbb', fontSize: 13 }}>or</div>
          <button style={styles.secondaryButton} onClick={() => setStep(1)}>
            Upload manually instead →
          </button>
        </>
      )}

      {step === 1 && (
        <>
          <div style={styles.title}>Request your Google export</div>
          {oauthStatus === 'success' && (
            <div style={styles.badge}>
              ✓ Google account connected. We will detect your export automatically.
            </div>
          )}
          <div style={styles.body}>
            Tap the button below — Google will pre-select YouTube and Spotify for you.
            You will receive a download link by email, usually within a few hours.
            {oauthStatus === 'success' && ' Once it arrives in your Drive, we will process it automatically.'}
          </div>
          <button style={styles.button} onClick={openTakeout}>
            Open Google Takeout
          </button>
          {oauthStatus === 'success' ? (
            <div style={styles.note}>
              Once Google processes your export it will appear in your Drive and
              we will pick it up automatically. You do not need to do anything else.
            </div>
          ) : (
            <button style={styles.secondaryButton} onClick={() => setStep(2)}>
              I already downloaded the file →
            </button>
          )}
          <button style={{ ...styles.secondaryButton, marginTop: 8 }} onClick={() => setStep(0)}>
            ← Back
          </button>
        </>
      )}

      {step === 2 && (
        <>
          <div style={styles.title}>Upload your export</div>
          <div style={styles.body}>
            Open the email from Google, download the .zip file, then select it below.
          </div>
          <label style={styles.fileBox}>
            {file ? `✓ ${file.name}` : 'Tap to select your .zip file'}
            <input
              type="file"
              accept=".zip"
              onChange={handleFileChange}
              style={{ display: 'none' }}
            />
          </label>
          <input
            style={styles.input}
            type="email"
            placeholder="Your Gmail address"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
          />
          {error && <div style={styles.error}>{error}</div>}
          <button
            style={{
              ...styles.button,
              opacity: (!file || !email.trim() || loading) ? 0.5 : 1,
              cursor: (!file || !email.trim() || loading) ? 'not-allowed' : 'pointer'
            }}
            onClick={handleUpload}
            disabled={!file || !email.trim() || loading}
          >
            {loading ? 'Processing your export...' : 'Upload'}
          </button>
          <button style={styles.secondaryButton} onClick={() => setStep(1)}>
            ← Back
          </button>
        </>
      )}

      {step === 3 && result && (
        <>
          <div style={styles.title}>You are all set</div>
          <div style={styles.success}>✓ Your data has been received and verified.</div>
          <div style={styles.body}>
            Your contribution has been recorded and your wallet has been credited.
          </div>
          <div style={styles.stat}>Watch events processed: <strong>{result.totalWatchEvents}</strong></div>
          <div style={styles.stat}>Data sources: <strong>{result.dataSourceTypes}</strong></div>
        </>
      )}
    </div>
  );
}