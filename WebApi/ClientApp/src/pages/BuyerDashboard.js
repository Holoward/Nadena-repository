import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Badge,
  Button,
  Card,
  CardBody,
  CardText,
  CardTitle,
  Col,
  Container,
  FormGroup,
  Input,
  Label,
  Row
} from 'reactstrap';
import { useAuth } from '../context/AuthContext';

const purchaseTypes = ['OneTime', 'Daily', 'Weekly', 'Monthly', 'Annual'];

const BuyerDashboard = () => {
  const { auth, logout } = useAuth();
  const [account, setAccount] = useState(null);
  const [pools, setPools] = useState([]);
  const [purchases, setPurchases] = useState([]);
  const [preview, setPreview] = useState(null);
  const [selectedPool, setSelectedPool] = useState(null);
  const [message, setMessage] = useState(null);
  const [filters, setFilters] = useState({ source: '', category: '', maxPrice: '' });
  const [form, setForm] = useState({
    purchaseType: 'OneTime',
    dataSources: ['YouTube'],
    recordCount: 1000,
    dateRangeStart: '',
    dateRangeEnd: ''
  });

  const load = async () => {
    const headers = { Authorization: `Bearer ${auth.token}` };
    const [accountRes, poolsRes, purchasesRes] = await Promise.all([
      fetch('/api/v1/Account/me', { headers }),
      fetch('/api/v1/DataPool', { headers }),
      fetch('/api/v1/DataClient/my-datasets', { headers })
    ]);

    if (accountRes.ok) setAccount((await accountRes.json()).data);
    if (poolsRes.ok) {
      const payload = await poolsRes.json();
      setPools(payload.data?.data || payload.data || []);
    }
    if (purchasesRes.ok) {
      const payload = await purchasesRes.json();
      setPurchases(payload.data || []);
    }
  };

  useEffect(() => {
    if (auth.token) {
      load().catch(() => setMessage({ type: 'danger', text: 'Failed to load dashboard.' }));
    }
  }, [auth.token]);

  const filteredPools = useMemo(() => pools.filter((pool) => {
    const matchesSource = !filters.source || (pool.sourceTable || '').toLowerCase().includes(filters.source.toLowerCase());
    const matchesCategory = !filters.category || pool.category === filters.category;
    const matchesPrice = !filters.maxPrice || pool.pricePerMonth <= Number(filters.maxPrice);
    return matchesSource && matchesCategory && matchesPrice;
  }), [filters, pools]);

  const livePrice = useMemo(() => {
    const base = (Math.max(form.recordCount, 100) / 1000) * 2;
    switch (form.purchaseType) {
      case 'Daily': return base * 0.8 * 30;
      case 'Weekly': return base * 0.75 * 4;
      case 'Monthly': return base * 0.7;
      case 'Annual': return base * 0.6 * 12;
      default: return base;
    }
  }, [form.recordCount, form.purchaseType]);

  const billingLabel = form.purchaseType === 'OneTime'
    ? 'Billed once'
    : form.purchaseType === 'Annual'
      ? 'Billed annually'
      : 'Billed monthly';

  const cancelSubscription = async (purchaseId) => {
    const response = await fetch(`/api/v1/DataClient/my-datasets/${purchaseId}/cancel`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${auth.token}` }
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Request completed.' });
    if (response.ok) await load();
  };

  const shareDataset = async (purchaseId) => {
    const email = window.prompt('Teammate email');
    if (!email) return;

    const response = await fetch(`/api/v1/DataClient/my-datasets/${purchaseId}/share`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${auth.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ email })
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Share request completed.' });
  };

  return (
    <div>
      <h2>Data Client Dashboard</h2>

      <Container>
        {account && !account.emailConfirmed && (
          <Alert color="warning">Please verify your email to unlock all features and complete purchases.</Alert>
        )}
        {message && <Alert color={message.type}>{message.text}</Alert>}

        <Card className="mb-4">
          <CardBody>
            <CardTitle tag="h4">Data Discovery</CardTitle>
            <Row>
              <Col md="3">
                <FormGroup>
                  <Label>Data source</Label>
                  <Input type="select" value={filters.source} onChange={(e) => setFilters({ ...filters, source: e.target.value })}>
                    <option value="">All</option>
                    <option value="youtube">YouTube</option>
                    <option value="spotify">Spotify</option>
                    <option value="netflix">Netflix</option>
                  </Input>
                </FormGroup>
              </Col>
              <Col md="3">
                <FormGroup>
                  <Label>Category</Label>
                  <Input value={filters.category} onChange={(e) => setFilters({ ...filters, category: e.target.value })} placeholder="e.g. Social Media" />
                </FormGroup>
              </Col>
              <Col md="3">
                <FormGroup>
                  <Label>Max monthly preview price</Label>
                  <Input type="number" value={filters.maxPrice} onChange={(e) => setFilters({ ...filters, maxPrice: e.target.value })} />
                </FormGroup>
              </Col>
            </Row>

            <Row>
              {filteredPools.map((pool) => (
                <Col md="6" lg="4" key={pool.id} className="mb-3">
                  <Card style={{ height: '100%' }}>
                    <CardBody>
                      <CardTitle tag="h5">{pool.name}</CardTitle>
                      <CardText>{pool.category}</CardText>
                      <CardText>
                        <strong>Records:</strong> {pool.approximateRecordCount?.toLocaleString?.() || 0}<br />
                        <strong>Last updated:</strong> {new Date(pool.created || Date.now()).toLocaleDateString()}<br />
                        <strong>Price preview:</strong> ${Number(pool.pricePerMonth || 0).toFixed(2)}
                      </CardText>
                      <div className="d-flex gap-2">
                        <a href="/marketplace" style={{color:'#6c47ff'}}>Purchase via Marketplace</a>
                      </div>
                    </CardBody>
                  </Card>
                </Col>
              ))}
            </Row>
          </CardBody>
        </Card>

        <Card>
          <CardBody>
            <CardTitle tag="h4">My Datasets</CardTitle>
            {purchases.length === 0 ? (
              <Alert color="light">No purchased datasets yet.</Alert>
            ) : (
              purchases.map((purchase) => (
                <Card key={purchase.id} className="mb-3">
                  <CardBody>
                    <div className="d-flex justify-content-between flex-wrap gap-3">
                      <div>
                        <h5 style={{ marginBottom: 4 }}>{purchase.invoiceNumber}</h5>
                        <div>
                          <Badge color="info">{purchase.purchaseType}</Badge>{' '}
                          <Badge color={purchase.status === 'Ready' ? 'success' : 'warning'}>{purchase.status}</Badge>
                        </div>
                        <small className="text-muted">
                          Purchased {new Date(purchase.purchasedAt).toLocaleString()} • {purchase.recordCount.toLocaleString()} records
                        </small>
                      </div>
                      <div className="text-md-end">
                        <div><strong>${Number(purchase.amountPaid || 0).toFixed(2)}</strong></div>
                        <div>{purchase.dataSources}</div>
                        {purchase.nextRefreshDate && <small className="text-muted">Next refresh: {new Date(purchase.nextRefreshDate).toLocaleDateString()}</small>}
                      </div>
                    </div>

                    <div className="mt-3">
                      <Button color="primary" size="sm" href={purchase.downloadUrl} target="_blank" rel="noreferrer">Download</Button>{' '}
                      <Button color="secondary" size="sm" href={`/api/v1/DataClient/my-datasets/${purchase.id}/invoice`} target="_blank" rel="noreferrer">Invoice PDF</Button>
                    </div>
                  </CardBody>
                </Card>
              ))
            )}
          </CardBody>
        </Card>
      </Container>
    </div>
  );
};

export default BuyerDashboard;
