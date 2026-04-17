import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, Button, Badge, Spinner, Alert, Form, FormGroup, Input } from 'reactstrap';
import { FaSearch, FaDatabase, FaDollarSign, FaCalendar, FaComments, FaLock } from 'react-icons/fa';

const Marketplace = () => {
    const [datasets, setDatasets] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [filters, setFilters] = useState({ language: '', category: '', minComments: '' });

    useEffect(() => {
        const fetchDatasets = async () => {
            try {
                const params = new URLSearchParams();
                if (filters.language) params.append('language', filters.language);
                if (filters.category) params.append('contentCategory', filters.category);
                if (filters.minComments) params.append('minCommentCount', filters.minComments);

                const response = await fetch(`/api/v1/Dataset?${params.toString()}`);
                const data = await response.json();

                if (response.ok) {
                    setDatasets(data.data?.items || data.data || []);
                }
            } catch (err) {
                setError('Failed to load datasets');
            } finally {
                setLoading(false);
            }
        };

        fetchDatasets();
    }, [filters]);

    return (
        <Container className="py-5" style={{ minHeight: '80vh' }}>
            <Row className="mb-4">
                <Col>
                    <h2 style={{ fontWeight: 'bold', color: '#00D1FF' }}>Browse Datasets</h2>
                    <p className="text-muted">Explore anonymized YouTube viewing data for research</p>
                </Col>
            </Row>

            {/* Filters */}
            <Row className="mb-4">
                <Col md="3">
                    <FormGroup>
                        <Input
                            type="select"
                            placeholder="Language"
                            value={filters.language}
                            onChange={(e) => setFilters({ ...filters, language: e.target.value })}
                        >
                            <option value="">All Languages</option>
                            <option value="en">English</option>
                            <option value="es">Spanish</option>
                            <option value="fr">French</option>
                            <option value="de">German</option>
                        </Input>
                    </FormGroup>
                </Col>
                <Col md="3">
                    <FormGroup>
                        <Input
                            type="select"
                            placeholder="Category"
                            value={filters.category}
                            onChange={(e) => setFilters({ ...filters, category: e.target.value })}
                        >
                            <option value="">All Categories</option>
                            <option value="Tech">Tech</option>
                            <option value="Entertainment">Entertainment</option>
                            <option value="Gaming">Gaming</option>
                            <option value="Music">Music</option>
                            <option value="Education">Education</option>
                        </Input>
                    </FormGroup>
                </Col>
                <Col md="3">
                    <FormGroup>
                        <Input
                            type="number"
                            placeholder="Min comments"
                            value={filters.minComments}
                            onChange={(e) => setFilters({ ...filters, minComments: e.target.value })}
                        />
                    </FormGroup>
                </Col>
            </Row>

            {/* Dataset List */}
            {loading ? (
                <div className="text-center py-5">
                    <Spinner color="primary" />
                </div>
            ) : error ? (
                <Alert color="danger">{error}</Alert>
            ) : datasets.length === 0 ? (
                <Alert color="info">No datasets available at this time.</Alert>
            ) : (
                <Row>
                    {datasets.map((dataset) => (
                        <Col md="4" key={dataset.id} className="mb-4">
                            <Card className="h-100" style={{ borderRadius: '12px' }}>
                                <CardBody>
                                    <h5 className="mb-2">{dataset.name || 'YouTube Viewing Data'}</h5>
                                    <Badge color="info" className="mb-2">{dataset.language || 'en'}</Badge>
                                    <Badge color="secondary" className="mb-2">{dataset.contentCategory || 'General'}</Badge>
                                    
                                    <div className="mt-3">
                                        <p className="mb-1"><FaComments className="mr-2" />{dataset.commentCount?.toLocaleString() || 0} comments</p>
                                        <p className="mb-1"><FaCalendar className="mr-2" />{dataset.dateRange || 'N/A'}</p>
                                        <p className="mb-1"><FaDollarSign className="mr-2" />${dataset.price || 0}</p>
                                    </div>

                                    <Button color="primary" block className="mt-3">
                                        <FaLock className="mr-2" />
                                        Request Access
                                    </Button>
                                </CardBody>
                            </Card>
                        </Col>
                    ))}
                </Row>
            )}
        </Container>
    );
};

export default Marketplace;