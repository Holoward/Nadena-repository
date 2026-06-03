import React, { useState } from 'react';
import { Container, Row, Col, Card, CardBody, Button, Form, Input, Spinner, Alert } from 'reactstrap';
import { Redirect, useHistory } from 'react-router-dom';
import axios from '../axiosConfig';
import { FaYoutube, FaSpotify } from 'react-icons/fa';
import { SiNetflix } from 'react-icons/si';
import { getStoredToken } from '../utils/authStorage';

const DataInterests = () => {
    const history = useHistory();
    const token = getStoredToken();
    
    const [selectedInterests, setSelectedInterests] = useState({});
    
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    if (!token) {
        return <Redirect to="/login" />;
    }

    const toggleInterest = (source) => {
        setSelectedInterests(prev => ({
            ...prev,
            [source]: !prev[source]
        }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        const activeInterests = Object.keys(selectedInterests).filter(k => selectedInterests[k]);
        
        if (activeInterests.length === 0) {
            setError("Please select at least one data interest.");
            setLoading(false);
            return;
        }

        try {
            const payload = {
                dataInterests: activeInterests
            };

            await axios.post('/api/v1/DataClient/setup', payload, {
                headers: { Authorization: `Bearer ${token}` }
            });

            history.push('/client/dashboard');
        } catch (err) {
            setError(err.response?.data?.message || 'Failed to save data interests. Please try again.');
            setLoading(false);
        }
    };

    return (
        <Container className="py-5" style={{ minHeight: '80vh', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
            <Row className="justify-content-center">
                <Col md="8" lg="6">
                    <div className="text-center mb-5">
                        <h2 className="font-weight-bold" style={{ color: '#00D1FF' }}>What kind of data are you looking for?</h2>
                        <p className="text-muted">Select the platforms you are interested in acquiring data from.</p>
                    </div>

                    {error && <Alert color="danger">{error}</Alert>}

                    <Form onSubmit={handleSubmit}>
                        {/* YouTube Card */}
                        <Card className={`mb-3 shadow-sm ${selectedInterests['YouTube'] ? 'border-primary' : ''}`} 
                              style={{ cursor: 'pointer', transition: '0.3s' }}
                              onClick={() => toggleInterest('YouTube')}>
                            <CardBody className="d-flex align-items-center">
                                <FaYoutube size={32} color="#FF0000" className="mr-3" />
                                <h5 className="mb-0 ml-3">YouTube</h5>
                                <div className="ml-auto">
                                    <Input type="checkbox" checked={!!selectedInterests['YouTube']} readOnly />
                                </div>
                            </CardBody>
                        </Card>
                        
                        {/* Spotify Card */}
                        <Card className={`mb-3 shadow-sm ${selectedInterests['Spotify'] ? 'border-primary' : ''}`}
                              style={{ cursor: 'pointer', transition: '0.3s' }}
                              onClick={() => toggleInterest('Spotify')}>
                            <CardBody className="d-flex align-items-center">
                                <FaSpotify size={32} color="#1DB954" className="mr-3" />
                                <h5 className="mb-0 ml-3">Spotify</h5>
                                <div className="ml-auto">
                                    <Input type="checkbox" checked={!!selectedInterests['Spotify']} readOnly />
                                </div>
                            </CardBody>
                        </Card>

                        {/* Netflix Card */}
                        <Card className={`mb-4 shadow-sm ${selectedInterests['Netflix'] ? 'border-primary' : ''}`}
                              style={{ cursor: 'pointer', transition: '0.3s' }}
                              onClick={() => toggleInterest('Netflix')}>
                            <CardBody className="d-flex align-items-center">
                                <SiNetflix size={32} color="#E50914" className="mr-3" />
                                <h5 className="mb-0 ml-3">Netflix</h5>
                                <div className="ml-auto">
                                    <Input type="checkbox" checked={!!selectedInterests['Netflix']} readOnly />
                                </div>
                            </CardBody>
                        </Card>
                        
                        <Button color="primary" block size="lg" type="submit" disabled={loading} style={{ background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)', border: 'none' }}>
                            {loading ? <Spinner size="sm" /> : 'Complete Setup'}
                        </Button>
                    </Form>
                </Col>
            </Row>
        </Container>
    );
};

export default DataInterests;
