import React, { useEffect, useMemo, useState } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, CardText, Button, FormGroup, Input, Alert, Badge } from 'reactstrap';
import { useAuth } from '../context/AuthContext';

const VolunteerDashboard = () => {
  const { auth, logout } = useAuth();
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

  const dataSourceCards = useMemo(() => ([
    { key: 'YouTube', color: '#FF0000', connectLabel: 'Connect via Google API' },
    { key: 'Spotify', color: '#1DB954', connectLabel: 'Connect via Spotify API' },
    { key: 'Netflix', color: '#E50914', connectLabel: 'Connect via Netflix API' }
  ]), []);

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
    return (
      <div style={{ padding: 40, textAlign: 'center' }}>
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
        <p className="mt-3">Loading your dashboard...</p>
      </div>
    );
  }

  return (
    <div>
      <div style={{ backgroundColor: '#1a2f4a', padding: '26px 0', color: 'white', marginBottom: '26px' }}>
        <Container>
          <Row className="align-items-center">
            <Col md="8">
              <h2 style={{ margin: 0 }}>Welcome, {firstName}!</h2>
              <p style={{ margin: '10px 0 0', opacity: 0.9 }}>Data Contributor Dashboard</p>
            </Col>
            <Col md="4" className="text-md-end">
              <Button color="light" onClick={logout} style={{ color: '#1a2f4a' }}>Logout</Button>
            </Col>
          </Row>
        </Container>
      </div>

      <Container>
        {account && !account.emailConfirmed && <Alert color="warning">Please verify your email to unlock all features.</Alert>}
        {error && <Alert color="danger">{error}</Alert>}

        <Card style={{ borderColor: '#1a2f4a', borderWidth: 2, marginBottom: 22 }}>
          <CardBody>
            <div className="d-flex justify-content-between align-items-center">
              <CardTitle tag="h4" style={{ color: '#1a2f4a', marginBottom: 0 }}>Data Sources</CardTitle>
              <Button color="primary" disabled={saving} onClick={saveDataSources} style={{ backgroundColor: '#1a2f4a', borderColor: '#1a2f4a' }}>
                {saving ? 'Saving...' : 'Save'}
              </Button>
            </div>
            <CardText className="mt-2" style={{ color: '#555' }}>
              Toggle what you want to share. You’ll always be notified based on your sharing preference.
            </CardText>

            <Row className="mt-3">
              {dataSourceCards.map((card) => {
                const st = settings[card.key];
                const connected = false;
                return (
                  <Col md="4" key={card.key} className="mb-3">
                    <Card style={{ borderRadius: 12, border: `2px solid ${st.enabled ? card.color : '#e9ecef'}` }}>
                      <CardBody>
                        <div className="d-flex justify-content-between align-items-center">
                          <div>
                            <h5 style={{ margin: 0 }}>{card.key}</h5>
                            <small style={{ color: '#666' }}>
                              Status: {connected ? <Badge color="success">Connected</Badge> : <Badge color="secondary">Not connected</Badge>}
                            </small>
                          </div>
                          <FormGroup check style={{ margin: 0 }}>
                            <label style={{ display: 'flex', alignItems: 'center', gap: 8, margin: 0 }}>
                              <Input type="checkbox" checked={!!st.enabled} onChange={() => handleToggle(card.key)} />
                              <span>Share</span>
                            </label>
                          </FormGroup>
                        </div>

                        {st.enabled && (
                          <div className="mt-3">
                            <Button color="outline-dark" block onClick={() => connectComingSoon(card.key)}>
                              {card.connectLabel} (Coming Soon)
                            </Button>

                            <div className="mt-3">
                              <div style={{ fontWeight: 600, marginBottom: 6 }}>Sharing preference</div>
                              <FormGroup check>
                                <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                                  <Input
                                    type="radio"
                                    name={`${card.key}-pref`}
                                    checked={st.sharingPreference === 'ShareNow'}
                                    onChange={() => handlePreference(card.key, 'ShareNow')}
                                  />
                                  <span>Share now and get notified when payment is processed</span>
                                </label>
                              </FormGroup>
                              <FormGroup check className="mt-1">
                                <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                                  <Input
                                    type="radio"
                                    name={`${card.key}-pref`}
                                    checked={st.sharingPreference === 'WaitForRequest'}
                                    onChange={() => handlePreference(card.key, 'WaitForRequest')}
                                  />
                                  <span>Wait until my data is requested, then I will be notified to approve the transfer</span>
                                </label>
                              </FormGroup>
                            </div>

                            <Alert color="info" className="mt-3" style={{ fontSize: 14 }}>
                              All personal identifiers are removed before any data leaves the platform.
                            </Alert>
                          </div>
                        )}
                      </CardBody>
                    </Card>
                  </Col>
                );
              })}
            </Row>
          </CardBody>
        </Card>

        <Row>
          <Col md="7" className="mb-3">
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
          </Col>

          <Col md="5" className="mb-3">
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
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default VolunteerDashboard;
