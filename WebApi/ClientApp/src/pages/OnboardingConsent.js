import React, { useEffect, useState } from 'react';
import { Container, Card, CardBody, Button, FormGroup, Input, Label, Alert } from 'reactstrap';
import { useHistory } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const OnboardingConsent = () => {
  const history = useHistory();
  const { auth } = useAuth();
  const [agreed, setAgreed] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    const load = async () => {
      try {
        const res = await fetch('/api/v1/Onboarding/status', {
          headers: { 'Authorization': `Bearer ${auth.token}` }
        });
        if (!res.ok) return;
        const data = await res.json();
        if (data.nextStep === '/onboarding/terms') history.replace('/onboarding/terms');
        if (!data.nextStep) history.replace('/contributor/dashboard');
      } catch (_) {
        // ignore
      }
    };
    if (auth.token) load();
  }, [auth.token, history]);

  const handleFinish = async () => {
    if (!agreed) {
      setError('You must give consent to continue.');
      return;
    }

    try {
      const res = await fetch('/api/v1/Onboarding/accept-consent', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${auth.token}` }
      });
      if (!res.ok) throw new Error('Failed to record consent.');
      history.push('/contributor/dashboard');
    } catch (e) {
      setError(e.message || 'Failed to finish onboarding.');
    }
  };

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#0f1f33', padding: '40px 0' }}>
      <Container style={{ maxWidth: 900 }}>
        <Card style={{ borderRadius: 12 }}>
          <CardBody style={{ padding: 28 }}>
            <h2 style={{ marginBottom: 14, color: '#1a2f4a' }}>Consent Form</h2>
            <div style={{ whiteSpace: 'pre-wrap', color: '#333', lineHeight: 1.6, background: '#f8f9fa', padding: 16, borderRadius: 8 }}>
              Data Consent Form — Coming Soon. This document will detail exactly what data is collected, how it is anonymized, and how it is used.
            </div>

            {error && <Alert color="danger" className="mt-3">{error}</Alert>}

            <FormGroup check className="mt-3">
              <Label check>
                <Input type="checkbox" checked={agreed} onChange={(e) => { setAgreed(e.target.checked); setError(null); }} />{' '}
                I give my informed consent to contribute my data under the terms described above
              </Label>
            </FormGroup>

            <div className="mt-4 d-flex justify-content-between">
              <Button color="link" onClick={() => history.push('/onboarding/terms')}>Back</Button>
              <Button color="primary" onClick={handleFinish} style={{ backgroundColor: '#1a2f4a', borderColor: '#1a2f4a' }}>
                Finish
              </Button>
            </div>
          </CardBody>
        </Card>
      </Container>
    </div>
  );
};

export default OnboardingConsent;
