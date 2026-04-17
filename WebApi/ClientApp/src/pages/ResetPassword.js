import React, { useState } from 'react';
import { Alert, Button, Card, CardBody, Col, Container, FormGroup, Input, Label, Row } from 'reactstrap';

const ResetPassword = () => {
  const params = new URLSearchParams(window.location.search);
  const [email, setEmail] = useState(params.get('email') || '');
  const [token, setToken] = useState(params.get('token') || '');
  const [newPassword, setNewPassword] = useState('');
  const [message, setMessage] = useState(null);
  const requestingLink = !token;

  const submit = async () => {
    const response = await fetch(requestingLink ? '/api/v1/Auth/forgot-password' : '/api/v1/Auth/reset-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestingLink ? { email } : { email, token, newPassword })
    });
    const payload = await response.json();
    setMessage({ type: response.ok ? 'success' : 'danger', text: payload.message || 'Password reset complete.' });
  };

  return (
    <Container className="py-5">
      <Row className="justify-content-center">
        <Col md="6">
          <Card body>
            <CardBody>
              <h2>{requestingLink ? 'Forgot Password' : 'Reset Password'}</h2>
              {message && <Alert color={message.type}>{message.text}</Alert>}
              <FormGroup><Label>Email</Label><Input value={email} onChange={(e) => setEmail(e.target.value)} /></FormGroup>
              {!requestingLink && (
                <>
                  <FormGroup><Label>Reset token</Label><Input value={token} onChange={(e) => setToken(e.target.value)} /></FormGroup>
                  <FormGroup><Label>New password</Label><Input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} /></FormGroup>
                </>
              )}
              <Button color="primary" onClick={submit}>{requestingLink ? 'Send reset link' : 'Set new password'}</Button>
            </CardBody>
          </Card>
        </Col>
      </Row>
    </Container>
  );
};

export default ResetPassword;
