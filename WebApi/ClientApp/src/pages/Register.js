import React, { useState } from 'react';
import { Link, useHistory } from 'react-router-dom';
import { Alert, Button, Card, CardBody, Col, Container, Form, FormGroup, Input, Label, Row } from 'reactstrap';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';

const Register = () => {
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    role: 'Data Contributor',
    paypalEmail: '',
    companyName: ''
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const history = useHistory();

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const validate = () => {
    const next = {};
    if (!formData.fullName.trim()) next.fullName = 'Full name is required';
    if (!/\S+@\S+\.\S+/.test(formData.email)) next.email = 'A valid email is required';
    if (formData.password.length < 8) next.password = 'Password must be at least 8 characters';
    if (formData.password !== formData.confirmPassword) next.confirmPassword = 'Passwords do not match';
    if (formData.role === 'Data Contributor' && !/\S+@\S+\.\S+/.test(formData.paypalEmail)) next.paypalEmail = 'PayPal email is required for data contributors';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const submit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    setErrors({});

    try {
      const requestData = {
        fullName: formData.fullName,
        email: formData.email,
        password: formData.password,
        role: formData.role,
        payPalEmail: formData.role === 'Data Contributor' ? formData.paypalEmail : '',
        companyName: formData.role === 'Data Client' ? formData.companyName : ''
      };
      const response = await axios.post('/api/v1/Auth/register', requestData);
      if (!response.data.success) {
        setErrors({ general: response.data.message || 'Registration failed.' });
        return;
      }

      const result = login(response.data.data);
      if (!result.success) {
        setErrors({ general: result.error || 'Registration completed but login failed.' });
        return;
      }

      history.push(result.role === 'Data Contributor' ? '/onboarding/terms' : '/client/dashboard');
    } catch (error) {
      setErrors({ general: error.response?.data?.message || 'Registration failed.' });
    } finally {
      setLoading(false);
    }
  };

  const isContributor = formData.role === 'Data Contributor';
  const isClient = formData.role === 'Data Client';

  return (
    <div style={{ minHeight: '100vh', padding: '40px 0', backgroundColor: '#1a2f4a' }}>
      <Container>
        <Row className="justify-content-center">
          <Col md="6" lg="5">
            <div className="text-center mb-4">
              <h1 style={{ color: 'white', fontWeight: 'bold', margin: 0 }}>NADENA</h1>
              <p style={{ color: 'rgba(255,255,255,0.7)', marginTop: 6 }}>Create your account</p>
            </div>
            <Card style={{ borderRadius: 12 }}>
              <CardBody className="p-4">
                {errors.general && <Alert color="danger">{errors.general}</Alert>}
                <Form onSubmit={submit}>
                  <FormGroup>
                    <Label>Full Name</Label>
                    <Input name="fullName" value={formData.fullName} onChange={handleChange} />
                    {errors.fullName && <small className="text-danger">{errors.fullName}</small>}
                  </FormGroup>
                  <FormGroup>
                    <Label>Email</Label>
                    <Input name="email" type="email" value={formData.email} onChange={handleChange} />
                    {errors.email && <small className="text-danger">{errors.email}</small>}
                  </FormGroup>
                  <FormGroup>
                    <Label>Role</Label>
                    <Input name="role" type="select" value={formData.role} onChange={handleChange}>
                      <option value="Data Contributor">Data Contributor</option>
                      <option value="Data Client">Data Client</option>
                    </Input>
                  </FormGroup>
                  {isContributor && (
                    <FormGroup>
                      <Label>PayPal Email</Label>
                      <Input name="paypalEmail" type="email" value={formData.paypalEmail} onChange={handleChange} />
                      {errors.paypalEmail && <small className="text-danger">{errors.paypalEmail}</small>}
                    </FormGroup>
                  )}
                  {isClient && (
                    <FormGroup>
                      <Label>Company Name</Label>
                      <Input name="companyName" value={formData.companyName} onChange={handleChange} placeholder="Company verification coming soon" />
                    </FormGroup>
                  )}
                  <FormGroup>
                    <Label>Password</Label>
                    <Input name="password" type="password" value={formData.password} onChange={handleChange} />
                    {errors.password && <small className="text-danger">{errors.password}</small>}
                  </FormGroup>
                  <FormGroup>
                    <Label>Confirm Password</Label>
                    <Input name="confirmPassword" type="password" value={formData.confirmPassword} onChange={handleChange} />
                    {errors.confirmPassword && <small className="text-danger">{errors.confirmPassword}</small>}
                  </FormGroup>
                  <Button color="primary" block disabled={loading}>{loading ? 'Creating account...' : 'Register'}</Button>
                </Form>
                <div className="text-center mt-3">
                  Already have an account? <Link to="/login">Login</Link>
                </div>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </div>
  );
};

export default Register;
