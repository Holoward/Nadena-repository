import React, { useEffect, useState } from 'react';
import { Container, Card, CardBody, CardTitle, CardText, Button, Alert, Input } from 'reactstrap';
import { useAuth } from '../context/AuthContext';

const VolunteerDashboard = () => {
  const { auth } = useAuth();
  const [profile, setProfile] = useState(null);
  const [settings, setSettings] = useState({
    YouTube: { enabled: true, sharingPreference: 'ShareNow' },
    Spotify: { enabled: false, sharingPreference: 'ShareNow' },
    Netflix: { enabled: false, sharingPreference: 'ShareNow' }
  });
  const [saving, setSaving] = useState(false);
  const [uploadHistory, setUploadHistory] = useState([]);
  const [earnings, setEarnings] = useState(null);
  const [account, setAccount] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const firstName = auth.user?.given_name || auth.user?.firstName || 'Data Contributor';

  const parseSettingsFromNotes = (notes) => {
    if (!notes) return null;
    try {
      const obj = JSON.parse(notes);
      const ds = obj?.dataSources;
      if (!ds) return null;
      return {
        YouTube: ds.YouTube || ds.youtube || null,
        Spotify: ds.Spotify || ds.spotify || null,
        Netflix: ds.Netflix || ds.netflix || null
      };
    } catch {
      return null;
    }
  };

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const [meRes, historyRes, earningsRes, accountRes] = await Promise.all([
          fetch('/api/v1/DataContributor/me', { headers: { 'Authorization': `Bearer ${auth.token}` } }),
          fetch('/api/v1/DataContributor/upload-history', { headers: { 'Authorization': `Bearer ${auth.token}` } }),
          fetch('/api/v1/DataContributor/earnings', { headers: { 'Authorization': `Bearer ${auth.token}` } }),
          fetch('/api/v1/Account/me', { headers: { 'Authorization': `Bearer ${auth.token}` } })
        ]);

        if (!meRes.ok) throw new Error('Failed to load profile.');
        const me = await meRes.json();
        setProfile(me.data);

        const parsed = parseSettingsFromNotes(me.data?.notes);
        if (parsed) {
          setSettings((prev) => ({
            YouTube: { enabled: !!parsed.YouTube?.enabled, sharingPreference: parsed.YouTube?.sharingPreference || prev.YouTube.sharingPreference },
            Spotify: { enabled: !!parsed.Spotify?.enabled, sharingPreference: parsed.Spotify?.sharingPreference || prev.Spotify.sharingPreference },
            Netflix: { enabled: !!parsed.Netflix?.enabled, sharingPreference: parsed.Netflix?.sharingPreference || prev.Netflix.sharingPreference }
          }));
        }

        if (historyRes.ok) {
          const h = await historyRes.json();
          setUploadHistory(h.data || []);
        }

        if (earningsRes.ok) {
          setEarnings(await earningsRes.json());
        }

        if (accountRes.ok) {
          const payload = await accountRes.json();
          setAccount(payload.data);
        }
      } catch (e) {
        setError(e.message || 'Failed to load dashboard.');
      } finally {
        setLoading(false);
      }
    };

    if (auth.token && auth.isAuthenticated) load();
  }, [auth.token, auth.isAuthenticated]);

  const handleToggle = (key) => {
    setSettings((prev) => ({ ...prev, [key]: { ...prev[key], enabled: !prev[key].enabled } }));
  };

  const handlePreference = (key, preference) => {
    setSettings((prev) => ({ ...prev, [key]: { ...prev[key], sharingPreference: preference } }));
  };

  const saveDataSources = async () => {
    setSaving(true);
    setError(null);
    try {
      const body = {
        youTube: { enabled: settings.YouTube.enabled, sharingPreference: settings.YouTube.sharingPreference },
        spotify: { enabled: settings.Spotify.enabled, sharingPreference: settings.Spotify.sharingPreference },
        netflix: { enabled: settings.Netflix.enabled, sharingPreference: settings.Netflix.sharingPreference }
      };

      const res = await fetch('/api/v1/DataContributor/data-sources', {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${auth.token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(body)
      });
      if (!res.ok) throw new Error('Failed to save settings.');
      const data = await res.json();
      setProfile(data.data);
    } catch (e) {
      setError(e.message || 'Failed to save.');
    } finally {
      setSaving(false);
    }
  };

  const connectComingSoon = (provider) => {
    window.alert(`${provider} connection is coming soon.`);
  };

  if (loading) {
    return <div style={{padding:40}}>Loading...</div>;
  }

  return (
    <div>
      <h2>Welcome, {firstName}</h2>

      <Container>
        {account && !account.emailConfirmed && <Alert color="warning">Please verify your email to unlock all features.</Alert>}
        {error && <Alert color="danger">{error}</Alert>}

        <p><a href="/upload" style={{color:'#6c47ff'}}>Submit your Google Takeout export →</a></p>

        <div>
          <div className="mb-3">
            <Card style={{ borderColor: '#1a2f4a', borderWidth: 2 }}>
              <CardBody>
                <CardTitle tag="h4" style={{ color: '#1a2f4a' }}>Upload History</CardTitle>
                <CardText style={{ color: '#666' }}>Your previous data contributions.</CardText>

                {uploadHistory.length === 0 ? (
                  <Alert color="light">No uploads yet.</Alert>
                ) : (
                  <div style={{ maxHeight: 320, overflow: 'auto' }}>
                    {uploadHistory.map((u, idx) => (
                      <div key={idx} style={{ padding: '10px 0', borderBottom: idx === uploadHistory.length - 1 ? 'none' : '1px solid #eee' }}>
                        <div className="d-flex justify-content-between">
                          <div style={{ fontWeight: 600 }}>{new Date(u.timestamp).toLocaleString()}</div>
                          <div style={{ color: '#666' }}>Ref: {u.entityId}</div>
                        </div>
                        <div style={{ fontSize: 13, color: '#666' }}>
                          {u.newValues ? `Details: ${u.newValues}` : 'Details: -'}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>
          </div>

          <div className="mb-3">
            <Card style={{ borderColor: '#1a2f4a', borderWidth: 2 }}>
              <CardBody>
                <CardTitle tag="h4" style={{ color: '#1a2f4a' }}>Earnings</CardTitle>
                {earnings ? (
                  <>
                    <div style={{ background: '#f8f9fa', borderRadius: 10, padding: 14 }}>
                      <div style={{ color: '#666' }}>Total lifetime earnings</div>
                      <div style={{ fontSize: 26, fontWeight: 700 }}>${(earnings.totals?.lifetimeEarnings || 0).toFixed(2)}</div>
                      <div className="mt-2" style={{ color: '#666' }}>Pending payments</div>
                      <div style={{ fontSize: 18, fontWeight: 700 }}>${(earnings.totals?.pendingPayments || 0).toFixed(2)}</div>
                    </div>
                    <div className="mt-3">
                      <div style={{ fontWeight: 600, marginBottom: 6 }}>Preferred payout method</div>
                      <Alert color="light" style={{ marginBottom: 0 }}>Coming soon</Alert>
                    </div>
                    <div className="mt-3">
                      <Button size="sm" color="secondary" href="/api/v1/DataContributor/earnings/export-csv" target="_blank" rel="noreferrer">Export earnings CSV</Button>{' '}
                      {profile && (
                        <Button
                          size="sm"
                          color="danger"
                          onClick={async () => {
                            const response = await fetch(`/api/v1/DataContributor/${profile.id}/my-data`, {
                              method: 'DELETE',
                              headers: { Authorization: `Bearer ${auth.token}` }
                            });
                            const payload = await response.json();
                            setError(payload.message || 'Deletion request submitted.');
                          }}
                        >
                          Request data deletion
                        </Button>
                      )}
                    </div>
                  </>
                ) : (
                  <Alert color="light">Earnings data unavailable.</Alert>
                )}
              </CardBody>
            </Card>
          </div>
        </div>
      </Container>
    </div>
  );
};

export default VolunteerDashboard;
