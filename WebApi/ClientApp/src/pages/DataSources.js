import React, { useState } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, Button, Form, FormGroup, Label, Input, Spinner, Alert } from 'reactstrap';
import { Redirect, useHistory } from 'react-router-dom';
import axios from '../axiosConfig';
import { FaYoutube, FaSpotify } from 'react-icons/fa';
import { SiNetflix } from 'react-icons/si';
import { getStoredToken } from '../utils/authStorage';

const DataSources = () => {
    const history = useHistory();
    const token = getStoredToken();
    
    const [selectedSources, setSelectedSources] = useState({});
    const [details, setDetails] = useState({
        youtubeAge: '',
        youtubeComments: '',
        spotifyPlaylists: '',
        netflixHours: ''
    });
    
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // If not authenticated, redirect to login
    if (!token) {
        return <Redirect to="/login" />;
    }

    const toggleSource = (source) => {
        setSelectedSources(prev => ({
            ...prev,
            [source]: !prev[source]
        }));
    };

    const handleDetailChange = (e) => {
        const { name, value } = e.target;
        setDetails(prev => ({
            ...prev,
            [name]: value
        }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        const activeSources = Object.keys(selectedSources).filter(k => selectedSources[k]);
        
        if (activeSources.length === 0) {
            setError("Please select at least one data source.");
            setLoading(false);
            return;
        }

        try {
            const payload = {
                dataSources: activeSources,
                youTubeDetails: activeSources.includes('YouTube') ? JSON.stringify({ accountAge: details.youtubeAge, commentCount: details.youtubeComments }) : null,
                spotifyDetails: activeSources.includes('Spotify') ? JSON.stringify({ playlists: details.spotifyPlaylists }) : null,
                netflixDetails: activeSources.includes('Netflix') ? JSON.stringify({ weeklyHours: details.netflixHours }) : null
            };

            await axios.post('/api/v1/DataContributor/setup', payload, {
                headers: { Authorization: `Bearer ${token}` }
            });

            history.push('/contributor/dashboard');
        } catch (err) {
            setError(err.response?.data?.message || 'Failed to save setup data. Please try again.');
            setLoading(false);
        }
    };

    return (
        <Container className="py-5" style={{ minHeight: '80vh', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
            <Row className="justify-content-center">
                <Col md="8" lg="6">
                    <div className="text-center mb-5">
                        <h2 className="font-weight-bold" style={{ color: '#00D1FF' }}>What data are you willing to share?</h2>
                        <p className="text-muted">Select the platforms you want to monetize and provide some basic details.</p>
                    </div>

                    {error && <Alert color="danger">{error}</Alert>}

                    <Form onSubmit={handleSubmit}>
                        {/* YouTube Card */}
                        <Card className={`mb-3 cursor-pointer shadow-sm ${selectedSources['YouTube'] ? 'border-primary' : ''}`} 
                              style={{ cursor: 'pointer', transition: '0.3s' }}
                              onClick={() => toggleSource('YouTube')}>
                            <CardBody className="d-flex align-items-center">
                                <FaYoutube size={32} color="#FF0000" className="mr-3" />
                                <h5 className="mb-0 ml-3">YouTube</h5>
                                <div className="ml-auto">
                                    <Input type="checkbox" checked={!!selectedSources['YouTube']} readOnly />
                                </div>
                            </CardBody>
                        </Card>
                        
                        {selectedSources['YouTube'] && (
                            <div className="p-3 mb-4 bg-light rounded border">
                                <FormGroup>
                                    <Label>YouTube Account Age</Label>
                                    <Input type="select" name="youtubeAge" value={details.youtubeAge} onChange={handleDetailChange} onClick={(e) => e.stopPropagation()}>
                                        <option value="">Select age</option>
                                        <option value="< 1 year">Less than 1 year</option>
                                        <option value="1-3 years">1-3 years</option>
                                        <option value="3-5 years">3-5 years</option>
                                        <option value="5+ years">5+ years</option>
                                    </Input>
                                </FormGroup>
                                <FormGroup>
                                    <Label>Estimated Comment Count (Total)</Label>
                                    <Input type="number" name="youtubeComments" placeholder="e.g. 500" value={details.youtubeComments} onChange={handleDetailChange} onClick={(e) => e.stopPropagation()}/>
                                </FormGroup>
                            </div>
                        )}

                        {/* Spotify Card */}
                        <Card className={`mb-3 cursor-pointer shadow-sm ${selectedSources['Spotify'] ? 'border-primary' : ''}`}
                              style={{ cursor: 'pointer', transition: '0.3s' }}
                              onClick={() => toggleSource('Spotify')}>
                            <CardBody className="d-flex align-items-center">
                                <FaSpotify size={32} color="#1DB954" className="mr-3" />
                                <h5 className="mb-0 ml-3">Spotify</h5>
                                <div className="ml-auto">
                                    <Input type="checkbox" checked={!!selectedSources['Spotify']} readOnly />
                                </div>
                            </CardBody>
                        </Card>

                        {selectedSources['Spotify'] && (
                            <div className="p-3 mb-4 bg-light rounded border">
                                <FormGroup>
                                    <Label>Estimated Number of Playlists</Label>
                                    <Input type="number" name="spotifyPlaylists" placeholder="e.g. 15" value={details.spotifyPlaylists} onChange={handleDetailChange} onClick={(e) => e.stopPropagation()} />
                                </FormGroup>
                            </div>
                        )}

                        {/* Netflix Card */}
                        <Card className={`mb-4 cursor-pointer shadow-sm ${selectedSources['Netflix'] ? 'border-primary' : ''}`}
                              style={{ cursor: 'pointer', transition: '0.3s' }}
                              onClick={() => toggleSource('Netflix')}>
                            <CardBody className="d-flex align-items-center">
                                <SiNetflix size={32} color="#E50914" className="mr-3" />
                                <h5 className="mb-0 ml-3">Netflix</h5>
                                <div className="ml-auto">
                                    <Input type="checkbox" checked={!!selectedSources['Netflix']} readOnly />
                                </div>
                            </CardBody>
                        </Card>
                        
                        {selectedSources['Netflix'] && (
                            <div className="p-3 mb-4 bg-light rounded border">
                                <FormGroup>
                                    <Label>Estimated Weekly Watch Hours</Label>
                                    <Input type="number" name="netflixHours" placeholder="e.g. 10" value={details.netflixHours} onChange={handleDetailChange} onClick={(e) => e.stopPropagation()} />
                                </FormGroup>
                            </div>
                        )}

                        <Button color="primary" block size="lg" type="submit" disabled={loading} style={{ background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)', border: 'none' }}>
                            {loading ? <Spinner size="sm" /> : 'Complete Setup'}
                        </Button>
                    </Form>
                </Col>
            </Row>
        </Container>
    );
};

export default DataSources;
