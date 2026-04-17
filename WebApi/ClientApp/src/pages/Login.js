import React, { useState } from 'react';
import { Link, useHistory } from 'react-router-dom';
import { Container, Row, Col, Card, CardBody, Form, FormGroup, Label, Input, Button, Alert } from 'reactstrap';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';

const Login = () => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [errors, setErrors] = useState({});
    const [loading, setLoading] = useState(false);

    const { login } = useAuth();
    const history = useHistory();

    const validateForm = () => {
        const newErrors = {};

        if (!email) {
            newErrors.email = 'Email is required';
        } else if (!/\S+@\S+\.\S+/.test(email)) {
            newErrors.email = 'Email is invalid';
        }

        if (!password) {
            newErrors.password = 'Password is required';
        } else if (password.length < 6) {
            newErrors.password = 'Password must be at least 6 characters';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!validateForm()) {
            return;
        }

        setLoading(true);
        setErrors({});

        try {
            const response = await axios.post('api/v1/Auth/login', {
                email,
                password
            });

            if (response.data.success) {
                const result = login(response.data.data);
                if (result.success) {
                    // Redirect based on role
                    switch (result.role) {
                        case 'Data Contributor':
                            history.push('/contributor/dashboard');
                            break;
                        case 'Data Client':
                            history.push('/client/dashboard');
                            break;
                        case 'Admin':
                            history.push('/admin/dashboard');
                            break;
                        default:
                            history.push('/');
                    }
                } else {
                    setErrors({ general: result.error || 'Login failed' });
                }
            } else {
                setErrors({ general: response.data.message || 'Login failed' });
            }
        } catch (err) {
            const errorMessage = err.response?.data?.message || err.response?.data?.messages || 'An error occurred. Please try again.';
            setErrors({ general: errorMessage });
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-page" style={{
            minHeight: '100vh',
            display: 'flex',
            alignItems: 'center',
            backgroundColor: '#1a2f4a'
        }}>
            <Container>
                <Row className="justify-content-center">
                    <Col md="5" lg="4">
                        {/* Nadena Logo */}
                        <div className="text-center mb-4">
                            <h1 style={{ color: 'white', fontWeight: 'bold', margin: 0 }}>
                                NADENA
                            </h1>
                            <p style={{ color: 'rgba(255,255,255,0.7)', margin: '5px 0 0' }}>
                                Welcome Back
                            </p>
                        </div>

                        <Card style={{ backgroundColor: 'white', border: 'none', borderRadius: '10px' }}>
                            <CardBody className="p-4">
                                {errors.general && (
                                    <Alert color="danger" className="mb-4">{errors.general}</Alert>
                                )}

                                <Form onSubmit={handleSubmit}>
                                    <FormGroup>
                                        <Label for="email" style={{ color: '#1a2f4a', fontWeight: '500' }}>Email</Label>
                                        <Input
                                            type="email"
                                            id="email"
                                            value={email}
                                            onChange={(e) => setEmail(e.target.value)}
                                            placeholder="Enter your email"
                                            autoComplete="email"
                                            style={{
                                                borderColor: errors.email ? '#dc3545' : '#1a2f4a',
                                                borderWidth: errors.email ? '2px' : '1px'
                                            }}
                                        />
                                        {errors.email && (
                                            <span style={{ color: '#dc3545', fontSize: '0.875rem' }}>{errors.email}</span>
                                        )}
                                    </FormGroup>

                                    <FormGroup>
                                        <Label for="password" style={{ color: '#1a2f4a', fontWeight: '500' }}>Password</Label>
                                        <Input
                                            type="password"
                                            id="password"
                                            value={password}
                                            onChange={(e) => setPassword(e.target.value)}
                                            placeholder="Enter your password"
                                            autoComplete="current-password"
                                            style={{
                                                borderColor: errors.password ? '#dc3545' : '#1a2f4a',
                                                borderWidth: errors.password ? '2px' : '1px'
                                            }}
                                        />
                                        {errors.password && (
                                            <span style={{ color: '#dc3545', fontSize: '0.875rem' }}>{errors.password}</span>
                                        )}
                                    </FormGroup>

                                    <Button
                                        type="submit"
                                        color="primary"
                                        block
                                        disabled={loading}
                                        style={{
                                            backgroundColor: '#1a2f4a',
                                            borderColor: '#1a2f4a',
                                            padding: '12px',
                                            fontWeight: '600'
                                        }}
                                    >
                                        {loading ? 'Logging in...' : 'Login'}
                                    </Button>
                                </Form>

                                <div className="text-center mt-4">
                                    <p style={{ marginBottom: 8 }}>
                                        <Link to="/reset-password" style={{ color: '#1a2f4a', fontWeight: '600' }}>
                                            Forgot password?
                                        </Link>
                                    </p>
                                    <p style={{ color: '#1a2f4a' }}>
                                        Don't have an account?{' '}
                                        <Link to="/register" style={{ color: '#1a2f4a', fontWeight: '600' }}>
                                            Register here
                                        </Link>
                                    </p>
                                </div>
                            </CardBody>
                        </Card>
                    </Col>
                </Row>
            </Container>
        </div>
    );
};

export default Login;
