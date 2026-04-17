import React, { useEffect, useState } from 'react';
import { Alert, Card, CardBody, Col, Container, Row } from 'reactstrap';

const VerifyEmail = () => {
  const [message, setMessage] = useState({ type: 'info', text: 'Verifying your email...' });

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    fetch('/api/v1/Auth/verify-email', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: params.get('email') || '', token: params.get('token') || '' })
    })
      .then(async (response) => {
        const payload = await response.json();
        setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Verification complete.' });
      })
      .catch(() => setMessage({ type: 'danger', text: 'Verification failed.' }));
  }, []);

  return (
    <Container className="py-5">
      <Row className="justify-content-center">
        <Col md="6">
          <Card body>
            <CardBody>
              <h2>Email Verification</h2>
              <Alert color={message.type}>{message.text}</Alert>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default VerifyEmail;
