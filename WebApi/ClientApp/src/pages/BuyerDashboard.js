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
  Modal,
  ModalBody,
  ModalFooter,
  ModalHeader,
  Row,
  Table
} from 'reactstrap';
import { useAuth } from '../context/AuthContext';

const purchaseTypes = ['OneTime', 'Daily', 'Weekly', 'Monthly', 'Annual'];

const MiniMetricsChart = ({ purchase }) => {
  if (purchase.purchaseType === 'One-time') {
    return null;
  }

  const history = JSON.parse(purchase.metricsHistoryJson || '[]');
  if (history.length === 0) {
    return null;
  }

  const width = 320;
  const height = 140;
  const max = Math.max(...history.map((item) => item.count), 1);
  const points = history.map((item, index) => {
    const x = (index / Math.max(history.length - 1, 1)) * (width - 20) + 10;
    const y = height - ((item.count / max) * (height - 20) + 10);
    return `${x},${y}`;
  }).join(' ');

  return (
    <svg viewBox={`0 0 ${width} ${height}`} style={{ width: '100%', height: 180 }}>
      <polyline fill="none" stroke="#1a2f4a" strokeWidth="3" points={points} />
      {history.map((item, index) => {
        const x = (index / Math.max(history.length - 1, 1)) * (width - 20) + 10;
        const y = height - ((item.count / max) * (height - 20) + 10);
        return <circle key={`${item.date}-${index}`} cx={x} cy={y} r="4" fill="#1a2f4a" />;
      })}
    </svg>
  );
};

const BuyerDashboard = () => {
  const { auth, logout } = useAuth();
  const [account, setAccount] = useState(null);
  const [pools, setPools] = useState([]);
  const [purchases, setPurchases] = useState([]);
  const [preview, setPreview] = useState(null);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [purchaseOpen, setPurchaseOpen] = useState(false);
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

  const openPurchase = (pool) => {
    setSelectedPool(pool);
    setPurchaseOpen(true);
  };

  const fetchPreview = async (poolId) => {
    const response = await fetch(`/api/v1/DataPool/${poolId}/preview`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    });
    if (!response.ok) {
      setMessage({ type: 'danger', text: 'Failed to load preview.' });
      return;
    }
    setPreview(await response.json());
    setPreviewOpen(true);
  };

  const submitPurchase = async () => {
    const endpoint = form.recordCount > 500000 ? '/api/v1/DataClient/request-custom-quote' : '/api/v1/DataClient/purchases';
    const response = await fetch(endpoint, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${auth.token}`,
        'Content-Type': 'application/json',
        'Idempotency-Key': crypto.randomUUID()
      },
      body: JSON.stringify(form.recordCount > 500000 ? {
        datasetName: selectedPool?.name || 'Custom dataset',
        recordCount: form.recordCount
      } : {
        poolName: selectedPool?.name,
        category: selectedPool?.category,
        purchaseType: form.purchaseType,
        dataSources: form.dataSources,
        dateRangeStart: form.dateRangeStart || null,
        dateRangeEnd: form.dateRangeEnd || null,
        recordCount: form.recordCount,
        contributorShareNow: true
      })
    });

    const payload = await response.json();
    if (!response.ok) {
      setMessage({ type: 'danger', text: payload.message || 'Purchase failed.' });
      return;
    }

    setMessage({ type: 'success', text: payload.message || 'Purchase confirmed.' });
    setPurchaseOpen(false);
    await load();
  };

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
      <div style={{ backgroundColor: '#1a2f4a', color: '#fff', padding: '24px 0', marginBottom: 24 }}>
        <Container>
          <div className="d-flex justify-content-between align-items-center">
            <div>
              <h2 style={{ margin: 0 }}>Data Client Dashboard</h2>
              <p style={{ margin: '8px 0 0', opacity: 0.85 }}>Discover pools, configure purchases, and manage your datasets.</p>
            </div>
            <Button color="light" onClick={logout}>Logout</Button>
          </div>
        </Container>
      </div>

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
                        <Button color="secondary" onClick={() => fetchPreview(pool.id)}>Preview</Button>
                        <Button color="primary" onClick={() => openPurchase(pool)}>Purchase</Button>
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
                      <Button color="secondary" size="sm" href={`/api/v1/DataClient/my-datasets/${purchase.id}/invoice`} target="_blank" rel="noreferrer">Invoice PDF</Button>{' '}
                      <Button color="info" size="sm" onClick={() => shareDataset(purchase.id)}>Share with teammate</Button>{' '}
                      {purchase.purchaseType !== 'One-time' && (
                        <Button color="danger" size="sm" onClick={() => cancelSubscription(purchase.id)}>Cancel subscription</Button>
                      )}
                    </div>

                    <div className="mt-3">
                      <MiniMetricsChart purchase={purchase} />
                    </div>
                  </CardBody>
                </Card>
              ))
            )}
          </CardBody>
        </Card>
      </Container>

      <Modal isOpen={previewOpen} toggle={() => setPreviewOpen(false)} size="lg">
        <ModalHeader toggle={() => setPreviewOpen(false)}>Anonymized Preview</ModalHeader>
        <ModalBody>
          <pre style={{ whiteSpace: 'pre-wrap', marginBottom: 0 }}>{JSON.stringify(preview?.data || preview, null, 2)}</pre>
        </ModalBody>
      </Modal>

      <Modal isOpen={purchaseOpen} toggle={() => setPurchaseOpen(false)} size="lg">
        <ModalHeader toggle={() => setPurchaseOpen(false)}>Subscription Builder</ModalHeader>
        <ModalBody>
          <Row>
            <Col md="6">
              <FormGroup>
                <Label>Purchase type</Label>
                <Input type="select" value={form.purchaseType} onChange={(e) => setForm({ ...form, purchaseType: e.target.value })}>
                  {purchaseTypes.map((type) => <option key={type} value={type}>{type === 'OneTime' ? 'One-time' : type}</option>)}
                </Input>
              </FormGroup>
              <FormGroup>
                <Label>Data sources included</Label>
                <Input type="select" multiple value={form.dataSources} onChange={(e) => setForm({ ...form, dataSources: Array.from(e.target.selectedOptions).map((option) => option.value) })}>
                  <option>YouTube</option>
                  <option>Spotify</option>
                  <option>Netflix</option>
                </Input>
              </FormGroup>
              <FormGroup>
                <Label>Record count: {form.recordCount.toLocaleString()}</Label>
                <Input type="range" min="100" max="1000000" step="100" value={form.recordCount} onChange={(e) => setForm({ ...form, recordCount: Number(e.target.value) })} />
              </FormGroup>
              <FormGroup>
                <Label>Date range start</Label>
                <Input type="date" value={form.dateRangeStart} onChange={(e) => setForm({ ...form, dateRangeStart: e.target.value })} />
              </FormGroup>
              <FormGroup>
                <Label>Date range end</Label>
                <Input type="date" value={form.dateRangeEnd} onChange={(e) => setForm({ ...form, dateRangeEnd: e.target.value })} />
              </FormGroup>
            </Col>
            <Col md="6">
              <Card body>
                <CardTitle tag="h5">Live pricing</CardTitle>
                <p><strong>Total price:</strong> ${livePrice.toFixed(2)}</p>
                <p><strong>Billing frequency:</strong> {billingLabel}</p>
                <p><strong>Included:</strong> {form.dataSources.join(', ') || 'No sources selected'} • {form.recordCount.toLocaleString()} records</p>
                {form.recordCount > 500000 && (
                  <Alert color="info">Large datasets are routed to a custom quote request.</Alert>
                )}
              </Card>
            </Col>
          </Row>
        </ModalBody>
        <ModalFooter>
          <Button color="secondary" onClick={() => setPurchaseOpen(false)}>Close</Button>
          <Button color="primary" onClick={submitPurchase}>{form.recordCount > 500000 ? 'Request custom quote' : 'Confirm purchase'}</Button>
        </ModalFooter>
      </Modal>
    </div>
  );
};

export default BuyerDashboard;
