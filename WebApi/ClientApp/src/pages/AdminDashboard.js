import React, { useEffect, useState } from 'react';
import { Alert, Badge, Button, Card, CardBody, CardTitle, Col, Container, Input, Row, Table } from 'reactstrap';
import { useAuth } from '../context/AuthContext';

const AdminDashboard = () => {
  const { auth, logout } = useAuth();
  const [data, setData] = useState({
    wallet: null,
    payouts: [],
    flagged: [],
    users: [],
    consent: [],
    revenue: null,
    deletions: [],
    emails: []
  });
  const [message, setMessage] = useState(null);
  const [roleFilter, setRoleFilter] = useState('');

  const headers = { Authorization: `Bearer ${auth.token}` };

  const load = async () => {
    const [wallet, payouts, flagged, users, consent, revenue, deletions, emails] = await Promise.all([
      fetch('/api/v1/Admin/platform-wallet', { headers }),
      fetch('/api/v1/Admin/pending-payouts', { headers }),
      fetch('/api/v1/Admin/flagged-datasets', { headers }),
      fetch(`/api/v1/Admin/users${roleFilter ? `?role=${encodeURIComponent(roleFilter)}` : ''}`, { headers }),
      fetch('/api/v1/Admin/consent-records', { headers }),
      fetch('/api/v1/Admin/revenue-report', { headers }),
      fetch('/api/v1/Admin/deletion-requests', { headers }),
      fetch('/api/v1/Admin/email-logs', { headers })
    ]);

    setData({
      wallet: wallet.ok ? (await wallet.json()).data : null,
      payouts: payouts.ok ? (await payouts.json()).data : [],
      flagged: flagged.ok ? (await flagged.json()).data : [],
      users: users.ok ? (await users.json()).data : [],
      consent: consent.ok ? (await consent.json()).data : [],
      revenue: revenue.ok ? (await revenue.json()).data : null,
      deletions: deletions.ok ? (await deletions.json()).data : [],
      emails: emails.ok ? (await emails.json()).data : []
    });
  };

  useEffect(() => {
    if (auth.token) load().catch(() => setMessage({ type: 'danger', text: 'Failed to load admin dashboard.' }));
  }, [auth.token, roleFilter]);

  const postAction = async (url, body) => {
    const response = await fetch(url, {
      method: 'POST',
      headers: { ...headers, 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : undefined
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Request completed.' });
    if (response.ok) await load();
  };

  return (
    <div>
      <div style={{ backgroundColor: '#1a2f4a', color: '#fff', padding: '24px 0', marginBottom: 24 }}>
        <Container>
          <div className="d-flex justify-content-between align-items-center">
            <div>
              <h2 style={{ margin: 0 }}>Admin Dashboard</h2>
              <p style={{ margin: '8px 0 0', opacity: 0.85 }}>Platform finances, reviews, users, and compliance tools.</p>
            </div>
            <Button color="light" onClick={logout}>Logout</Button>
          </div>
        </Container>
      </div>

      <Container>
        {message && <Alert color={message.type}>{message.text}</Alert>}

        <Card className="mb-4" body style={{ background: '#f8f9fa' }}>
          <CardTitle tag="h4">Platform Wallet Balance</CardTitle>
          <h1>${Number(data.wallet?.balance || 0).toFixed(2)}</h1>
          <div>Pending balance: ${Number(data.wallet?.pendingBalance || 0).toFixed(2)}</div>
        </Card>

        <Row>
          <Col lg="6" className="mb-4">
            <Card body>
              <CardTitle tag="h4">Pending Contributor Payouts</CardTitle>
              <Table responsive size="sm">
                <thead><tr><th>Contributor</th><th>Amount</th><th>Status</th><th /></tr></thead>
                <tbody>
                  {data.payouts.map((item) => (
                    <tr key={item.id}>
                      <td>{item.contributorName}</td>
                      <td>${Number(item.amount).toFixed(2)}</td>
                      <td><Badge color={item.status === 'Completed' ? 'success' : 'warning'}>{item.status}</Badge></td>
                      <td><Button size="sm" color="primary" onClick={() => postAction(`/api/v1/Admin/pending-payouts/${item.id}/mark-disbursed`, { notes: 'Manually transferred' })}>Mark as disbursed</Button></td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card>
          </Col>

          <Col lg="6" className="mb-4">
            <Card body>
              <CardTitle tag="h4">Flagged Datasets</CardTitle>
              <Table responsive size="sm">
                <thead><tr><th>Dataset</th><th>Reason</th><th /></tr></thead>
                <tbody>
                  {data.flagged.map((item) => (
                    <tr key={item.id}>
                      <td>{item.title}</td>
                      <td>{item.integrityReason}</td>
                      <td>
                        <Button size="sm" color="success" onClick={() => postAction(`/api/v1/Admin/flagged-datasets/${item.id}/clear`)}>Clear</Button>{' '}
                        <Button size="sm" color="danger" onClick={() => postAction(`/api/v1/Admin/flagged-datasets/${item.id}/reject`)}>Reject</Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card>
          </Col>
        </Row>

        <Row>
          <Col lg="6" className="mb-4">
            <Card body>
              <CardTitle tag="h4">User Management</CardTitle>
              <Input className="mb-3" type="select" value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)}>
                <option value="">All roles</option>
                <option value="Data Contributor">Data Contributor</option>
                <option value="Data Client">Data Client</option>
                <option value="Admin">Admin</option>
              </Input>
              <Table responsive size="sm">
                <thead><tr><th>Name</th><th>Role</th><th>Status</th><th /></tr></thead>
                <tbody>
                  {data.users.map((user) => (
                    <tr key={user.id}>
                      <td>{user.fullName}<div className="text-muted small">{user.email}</div></td>
                      <td>{user.role}</td>
                      <td><Badge color={user.isSuspended ? 'danger' : 'success'}>{user.isSuspended ? 'Suspended' : 'Active'}</Badge></td>
                      <td>
                        {user.isSuspended
                          ? <Button size="sm" color="success" onClick={() => postAction(`/api/v1/Admin/users/${user.id}/reactivate`)}>Reactivate</Button>
                          : <Button size="sm" color="warning" onClick={() => postAction(`/api/v1/Admin/users/${user.id}/suspend`)}>Suspend</Button>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card>
          </Col>

          <Col lg="6" className="mb-4">
            <Card body>
              <CardTitle tag="h4">Revenue Report</CardTitle>
              <p><strong>Total revenue:</strong> ${Number(data.revenue?.totalRevenue || 0).toFixed(2)}</p>
              <p><strong>Platform fees:</strong> ${Number(data.revenue?.platformFees || 0).toFixed(2)}</p>
              <p><strong>Contributor payouts:</strong> ${Number(data.revenue?.contributorPayouts || 0).toFixed(2)}</p>
              <Button color="secondary" href="/api/v1/Admin/revenue-report?exportCsv=true" target="_blank" rel="noreferrer">Export CSV</Button>
            </Card>
          </Col>
        </Row>

        <Row>
          <Col lg="6" className="mb-4">
            <Card body>
              <CardTitle tag="h4">Consent Audit Log</CardTitle>
              <Table responsive size="sm">
                <thead><tr><th>User</th><th>Document</th><th>Version</th><th>Date</th></tr></thead>
                <tbody>
                  {data.consent.slice(0, 15).map((item) => (
                    <tr key={item.id}>
                      <td>{item.userId}</td>
                      <td>{item.documentType}</td>
                      <td>{item.formVersion}</td>
                      <td>{new Date(item.agreedAt).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card>
          </Col>

          <Col lg="6" className="mb-4">
            <Card body>
              <CardTitle tag="h4">Pending Deletion Requests</CardTitle>
              <Table responsive size="sm">
                <thead><tr><th>User</th><th>Status</th><th>Requested</th><th /></tr></thead>
                <tbody>
                  {data.deletions.map((item) => (
                    <tr key={item.id}>
                      <td>{item.userId}</td>
                      <td>{item.status}</td>
                      <td>{new Date(item.requestedAt).toLocaleString()}</td>
                      <td>
                        {item.status === 'Pending' && (
                          <>
                            <Button size="sm" color="success" onClick={() => postAction(`/api/v1/Admin/deletion-requests/${item.id}/approve`)}>Approve</Button>{' '}
                            <Button size="sm" color="danger" onClick={() => postAction(`/api/v1/Admin/deletion-requests/${item.id}/deny`)}>Deny</Button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card>
          </Col>
        </Row>

        <Card body className="mb-4">
          <CardTitle tag="h4">Email Log Viewer</CardTitle>
          <Table responsive size="sm">
            <thead><tr><th>To</th><th>Subject</th><th>Status</th><th>Sent</th></tr></thead>
            <tbody>
              {data.emails.slice(0, 25).map((item) => (
                <tr key={item.id}>
                  <td>{item.to}</td>
                  <td>{item.subject}</td>
                  <td>{item.status}</td>
                  <td>{new Date(item.sentAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </Card>
      </Container>
    </div>
  );
};

export default AdminDashboard;
