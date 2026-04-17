import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, CardBody, CardTitle, Col, Container, FormGroup, Input, Label, Row, Table } from 'reactstrap';
import { useAuth } from '../context/AuthContext';

const AccountSettings = () => {
  const { auth, logout } = useAuth();
  const [message, setMessage] = useState(null);
  const [account, setAccount] = useState(null);
  const [settings, setSettings] = useState({ fullName: '', email: '', companyName: '' });
  const [passwords, setPasswords] = useState({ currentPassword: '', newPassword: '' });
  const [preferences, setPreferences] = useState([]);
  const [sessions, setSessions] = useState([]);

  const load = async () => {
    const headers = { Authorization: `Bearer ${auth.token}` };
    const [accountRes, prefsRes, sessionsRes] = await Promise.all([
      fetch('/api/v1/Account/me', { headers }),
      fetch('/api/v1/Account/notification-preferences', { headers }),
      fetch('/api/v1/Account/sessions', { headers })
    ]);

    if (accountRes.ok) {
      const payload = await accountRes.json();
      setAccount(payload.data);
      setSettings({
        fullName: payload.data.fullName || '',
        email: payload.data.email || '',
        companyName: payload.data.companyName || ''
      });
    }
    if (prefsRes.ok) setPreferences((await prefsRes.json()).data || []);
    if (sessionsRes.ok) setSessions((await sessionsRes.json()).data || []);
  };

  useEffect(() => {
    if (auth.token) load();
  }, [auth.token]);

  const saveSettings = async () => {
    const response = await fetch('/api/v1/Account/settings', {
      method: 'PUT',
      headers: { Authorization: `Bearer ${auth.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify(settings)
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Settings updated.' });
  };

  const changePassword = async () => {
    const response = await fetch('/api/v1/Account/change-password', {
      method: 'POST',
      headers: { Authorization: `Bearer ${auth.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify(passwords)
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Password updated.' });
  };

  const savePreferences = async () => {
    const response = await fetch('/api/v1/Account/notification-preferences', {
      method: 'PUT',
      headers: { Authorization: `Bearer ${auth.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify(preferences)
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Preferences updated.' });
  };

  const logoutAll = async () => {
    const response = await fetch('/api/v1/Account/sessions/logout-all', {
      method: 'POST',
      headers: { Authorization: `Bearer ${auth.token}` }
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'All sessions revoked.' });
    if (response.ok) logout();
  };

  const deleteAccount = async () => {
    if (!window.confirm('Schedule account deletion?')) return;
    const response = await fetch('/api/v1/Account/delete', {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${auth.token}` }
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Deletion scheduled.' });
  };

  return (
    <Container className="py-4">
      <h2>Account Settings</h2>
      {account && !account.emailConfirmed && <Alert color="warning">Please verify your email to unlock all features.</Alert>}
      {message && <Alert color={message.type}>{message.text}</Alert>}
      <Row>
        <Col md="6" className="mb-4">
          <Card body>
            <CardTitle tag="h4">Profile</CardTitle>
            <FormGroup><Label>Display name</Label><Input value={settings.fullName} onChange={(e) => setSettings({ ...settings, fullName: e.target.value })} /></FormGroup>
            <FormGroup><Label>Email</Label><Input value={settings.email} onChange={(e) => setSettings({ ...settings, email: e.target.value })} /></FormGroup>
            <FormGroup><Label>Company name</Label><Input value={settings.companyName} onChange={(e) => setSettings({ ...settings, companyName: e.target.value })} /></FormGroup>
            <Button color="primary" onClick={saveSettings}>Save profile</Button>
          </Card>
        </Col>
        <Col md="6" className="mb-4">
          <Card body>
            <CardTitle tag="h4">Change Password</CardTitle>
            <FormGroup><Label>Current password</Label><Input type="password" value={passwords.currentPassword} onChange={(e) => setPasswords({ ...passwords, currentPassword: e.target.value })} /></FormGroup>
            <FormGroup><Label>New password</Label><Input type="password" value={passwords.newPassword} onChange={(e) => setPasswords({ ...passwords, newPassword: e.target.value })} /></FormGroup>
            <Button color="primary" onClick={changePassword}>Update password</Button>
          </Card>
        </Col>
      </Row>
      <Row>
        <Col md="6" className="mb-4">
          <Card body>
            <CardTitle tag="h4">Notification Preferences</CardTitle>
            {preferences.map((item, index) => (
              <FormGroup check className="mb-2" key={item.eventType}>
                <Label check>
                  <Input type="checkbox" checked={item.isEnabled} onChange={(e) => {
                    const next = [...preferences];
                    next[index] = { ...item, isEnabled: e.target.checked };
                    setPreferences(next);
                  }} />{' '}
                  {item.eventType}
                </Label>
              </FormGroup>
            ))}
            <Button color="primary" onClick={savePreferences}>Save preferences</Button>
          </Card>
        </Col>
        <Col md="6" className="mb-4">
          <Card body>
            <CardTitle tag="h4">Sessions</CardTitle>
            <Table size="sm" responsive>
              <thead><tr><th>Created</th><th>Expires</th><th>Status</th></tr></thead>
              <tbody>
                {sessions.map((item) => (
                  <tr key={item.id}>
                    <td>{new Date(item.created).toLocaleString()}</td>
                    <td>{new Date(item.expiresAt).toLocaleString()}</td>
                    <td>{item.isActive ? 'Active' : 'Revoked'}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
            <Button color="warning" onClick={logoutAll}>Log out all devices</Button>
          </Card>
        </Col>
      </Row>
      <Card body>
        <CardTitle tag="h4">Delete Account</CardTitle>
        <p>Soft delete keeps data for 30 days before permanent cleanup.</p>
        <Button color="danger" onClick={deleteAccount}>Schedule deletion</Button>
      </Card>
    </Container>
  );
};

export default AccountSettings;
