import React, { useEffect, useState } from 'react';
import { Container, Card, CardBody, Button, FormGroup, Input, Label, Alert } from 'reactstrap';
import { useHistory } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const OnboardingTerms = () => {
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
        if (data.nextStep === '/onboarding/consent') history.replace('/onboarding/consent');
        if (!data.nextStep) history.replace('/contributor/dashboard');
      } catch (_) {
        // ignore
      }
    };
    if (auth.token) load();
  }, [auth.token, history]);

  const handleContinue = async () => {
    if (!agreed) {
      setError('You must agree to continue.');
      return;
    }

    try {
      const res = await fetch('/api/v1/Onboarding/accept-terms', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${auth.token}` }
      });
      if (!res.ok) throw new Error('Failed to record acceptance.');
      history.push('/onboarding/consent');
    } catch (e) {
      setError(e.message || 'Failed to continue.');
    }
  };

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#0f1f33', padding: '40px 0' }}>
      <Container style={{ maxWidth: 900 }}>
        <Card style={{ borderRadius: 12 }}>
          <CardBody style={{ padding: 28 }}>
            <h2 style={{ marginBottom: 14, color: '#1a2f4a' }}>Terms of Service</h2>
            <div style={{ whiteSpace: 'pre-wrap', color: '#333', lineHeight: 1.6, background: '#f8f9fa', padding: 16, borderRadius: 8 }}>
              Terms of Service — Coming Soon. This document will be updated before public launch.
            </div>

            {error && <Alert color="danger" className="mt-3">{error}</Alert>}

            <FormGroup check className="mt-3">
              <Label check>
                <Input type="checkbox" checked={agreed} onChange={(e) => { setAgreed(e.target.checked); setError(null); }} />{' '}
                I have read and agree to the Terms of Service
              </Label>
            </FormGroup>

            <div className="mt-4 d-flex justify-content-end">
              <Button color="primary" onClick={handleContinue} style={{ backgroundColor: '#1a2f4a', borderColor: '#1a2f4a' }}>
                Continue
              </Button>
            </div>
          </CardBody>
        </Card>
      </Container>
    </div>
  );
};

export default OnboardingTerms;
