import React, { useEffect, useState } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, Button, Alert, Spinner, Form, FormGroup, Label, Input, Badge, CustomInput } from 'reactstrap';
import { useHistory } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { FaChartBar, FaClock, FaVideo, FaTv, FaCalendar, FaHeart, FaPercentage, FaShieldAlt, FaCheck, FaEnvelope, FaPoll } from 'react-icons/fa';
import SurveyStep from '../components/SurveyStep';

const ContributorStatsPage = () => {
    const history = useHistory();
    const { auth } = useAuth();
    const [loading, setLoading] = useState(true);
    const [stats, setStats] = useState(null);
    const [consentText, setConsentText] = useState('');
    const [consentLoading, setConsentLoading] = useState(true);
    const [error, setError] = useState(null);
    const [consented, setConsented] = useState(false);
    const [email, setEmail] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const [submitted, setSubmitted] = useState(false);
    const [submitError, setSubmitError] = useState(null);
    const [surveyCompleted, setSurveyCompleted] = useState(false);
    const [surveySkipped, setSurveySkipped] = useState(false);
    const [currentStep, setCurrentStep] = useState(1);

    const userId = auth.user?.id || auth.user?.sub;

    useEffect(() => {
        const loadData = async () => {
            const storedStats = sessionStorage.getItem('contributorStats');
            if (storedStats) {
                try {
                    setStats(JSON.parse(storedStats));
                } catch (e) {
                    console.error('Failed to parse stored stats:', e);
                }
            }

            if (!auth.token || !userId) {
                setError('Please log in to view your stats.');
                setLoading(false);
                setConsentLoading(false);
                return;
            }

            try {
                const consentResponse = await fetch('/api/v1/legal/consent-form');
                if (consentResponse.ok) {
                    const consentData = await consentResponse.json();
                    setConsentText(consentData.content || consentData.data?.content || '');
                }
            } catch (e) {
                console.error('Failed to load consent text:', e);
            } finally {
                setConsentLoading(false);
            }

            try {
                if (!storedStats) {
                    const response = await fetch(`/api/v1/takeout/stats/${userId}`, {
                        headers: { 'Authorization': `Bearer ${auth.token}` }
                    });

                    const data = await response.json();

                    if (!response.ok) {
                        throw new Error(data.message || 'Failed to load stats.');
                    }

                    setStats(data.data || data);
                }
            } catch (err) {
                setError(err.message || 'Failed to load stats. Please upload your Takeout first.');
            } finally {
                setLoading(false);
            }
        };

        loadData();
    }, [auth.token, userId]);

    const handleConsentChange = (e) => {
        setConsented(e.target.checked);
        if (!e.target.checked) {
            setEmail('');
        }
    };

    const handleProceedToSurvey = () => {
        setCurrentStep(3);
    };

    const handleSurveyComplete = () => {
        setSurveyCompleted(true);
        setCurrentStep(4);
    };

    const handleSurveySkip = () => {
        setSurveySkipped(true);
        setCurrentStep(4);
    };

    const handleSubmit = async () => {
        if (!consented) {
            setSubmitError('Please consent to share your data.');
            return;
        }

        setSubmitting(true);
        setSubmitError(null);

        try {
            const donationResponse = await fetch('/api/v1/donation/create', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${auth.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    contributorId: userId,
                    consentVersion: 'v1.0'
                })
            });

            const donationData = await donationResponse.json();

            if (!donationResponse.ok) {
                throw new Error(donationData.message || 'Failed to submit donation.');
            }

            if (email && email.trim()) {
                try {
                    await fetch('/api/v1/contributor/email', {
                        method: 'PUT',
                        headers: {
                            'Authorization': `Bearer ${auth.token}`,
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            email: email.trim()
                        })
                    });
                } catch (emailError) {
                    console.error('Failed to save email:', emailError);
                }
            }

            sessionStorage.removeItem('contributorStats');
            setSubmitted(true);
        } catch (err) {
            setSubmitError(err.message || 'An error occurred. Please try again.');
        } finally {
            setSubmitting(false);
        }
    };

    if (loading || consentLoading) {
        return (
            <Container className="py-5" style={{ minHeight: '80vh' }}>
                <Row className="justify-content-center">
                    <Col md="10" lg="8" className="text-center">
                        <Spinner color="primary" size="lg" />
                        <p className="mt-3">Loading...</p>
                    </Col>
                </Row>
            </Container>
        );
    }

    if (error) {
        return (
            <Container className="py-5">
                <Row className="justify-content-center">
                    <Col md="10" lg="8">
                        <Alert color="danger">{error}</Alert>
                        <Button color="primary" href="/upload">
                            Upload Your Takeout Data
                        </Button>
                    </Col>
                </Row>
            </Container>
        );
    }

    const totalHoursWatched = stats ? Math.round((stats.totalVideos * 8) / 60) : 0;
    const peakHourFormatted = stats ? `${stats.peakHour}:00` : '';
    const dayNames = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

    const topCategories = stats?.categoryPercentages
        ? Object.entries(stats.categoryPercentages)
            .sort((a, b) => b[1] - a[1])
            .slice(0, 3)
            .map(([category, percentage]) => ({ category, percentage }))
        : [];

    const maxHourValue = stats ? Math.max(...(stats.hourDistribution || [0])) : 0;
    const maxDayValue = stats ? Math.max(...(stats.dayDistribution || [0])) : 0;

    const renderStepIndicator = () => {
        const steps = [
            { num: 1, label: 'Your Stats', icon: FaChartBar },
            { num: 2, label: 'Consent', icon: FaShieldAlt },
            { num: 3, label: 'Survey', icon: FaPoll },
            { num: 4, label: 'Submit', icon: FaCheck }
        ];

        return (
            <div className="d-flex justify-content-center mb-4">
                {steps.map((step, idx) => (
                    <div key={step.num} className="d-flex align-items-center">
                        <div className={`text-center ${currentStep >= step.num ? 'text-primary' : 'text-muted'}`}>
                            <div className={`rounded-circle d-inline-flex align-items-center justify-content-center ${currentStep === step.num ? 'bg-primary text-white' : currentStep > step.num ? 'bg-success text-white' : 'bg-light'}`}
                                style={{ width: 36, height: 36 }}>
                                {currentStep > step.num ? <FaCheck size={14} /> : <step.icon size={14} />}
                            </div>
                            <div style={{ fontSize: 11 }}>{step.label}</div>
                        </div>
                        {idx < steps.length - 1 && (
                            <div className={`mx-2 ${currentStep > step.num ? 'bg-success' : 'bg-light'}`} style={{ width: 30, height: 2 }} />
                        )}
                    </div>
                ))}
            </div>
        );
    };

    const renderStep1Stats = () => (
        <>
            <div className="text-center mb-4">
                <h2 className="font-weight-bold" style={{ color: '#00D1FF' }}>
                    Your YouTube Stats
                </h2>
            </div>

            <Alert color="info" className="mb-4">
                <FaHeart className="mr-2" />
                Here is what will be shared if you consent.
            </Alert>

            <Row className="mb-3">
                <Col md="3" className="mb-3">
                    <Card className="text-center h-100" style={{ borderRadius: 12 }}>
                        <CardBody>
                            <FaClock size={24} color="#00D1FF" className="mb-2" />
                            <div style={{ fontSize: 24, fontWeight: 700 }}>{totalHoursWatched}</div>
                            <div className="text-muted">Hours Watched</div>
                        </CardBody>
                    </Card>
                </Col>
                <Col md="3" className="mb-3">
                    <Card className="text-center h-100" style={{ borderRadius: 12 }}>
                        <CardBody>
                            <FaVideo size={24} color="#FF0000" className="mb-2" />
                            <div style={{ fontSize: 24, fontWeight: 700 }}>{stats?.totalVideos?.toLocaleString() || 0}</div>
                            <div className="text-muted">Total Videos</div>
                        </CardBody>
                    </Card>
                </Col>
                <Col md="3" className="mb-3">
                    <Card className="text-center h-100" style={{ borderRadius: 12 }}>
                        <CardBody>
                            <FaTv size={24} color="#1DB954" className="mb-2" />
                            <div style={{ fontSize: 24, fontWeight: 700 }}>{stats?.uniqueChannels?.toLocaleString() || 0}</div>
                            <div className="text-muted">Unique Channels</div>
                        </CardBody>
                    </Card>
                </Col>
                <Col md="3" className="mb-3">
                    <Card className="text-center h-100" style={{ borderRadius: 12 }}>
                        <CardBody>
                            <FaCalendar size={24} color="#E50914" className="mb-2" />
                            <div style={{ fontSize: 24, fontWeight: 700 }}>{stats?.historyDays || 0}</div>
                            <div className="text-muted">Days Span</div>
                        </CardBody>
                    </Card>
                </Col>
            </Row>

            <Card className="mb-4 shadow-sm">
                <CardBody>
                    <CardTitle tag="h6">
                        <FaPercentage className="mr-2" />
                        Top Categories
                    </CardTitle>
                    <div className="d-flex flex-wrap gap-2">
                        {topCategories.map((cat, idx) => (
                            <Badge key={idx} color="primary" style={{ fontSize: 14, padding: '8px 12px' }}>
                                {cat.category} ({cat.percentage?.toFixed(1)}%)
                            </Badge>
                        ))}
                        {topCategories.length === 0 && <span className="text-muted">No category data</span>}
                    </div>
                </CardBody>
            </Card>

            <Button
                color="primary"
                block
                size="lg"
                onClick={() => setCurrentStep(2)}
                style={{
                    background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)',
                    border: 'none'
                }}
            >
                Continue to Consent
            </Button>
        </>
    );

    const renderStep2Consent = () => (
        <Card className="shadow-sm" style={{ borderColor: '#00D1FF', borderWidth: 2 }}>
            <CardBody>
                <CardTitle tag="h5" className="mb-3">
                    <FaShieldAlt className="mr-2" />
                    Consent to Share
                </CardTitle>

                <div
                    className="mb-4 p-3"
                    style={{
                        maxHeight: '200px',
                        overflowY: 'auto',
                        backgroundColor: '#f8f9fa',
                        borderRadius: 8,
                        fontSize: 13,
                        whiteSpace: 'pre-wrap'
                    }}
                >
                    {consentText || 'Loading consent text...'}
                </div>

                <FormGroup className="mb-3">
                    <CustomInput
                        type="checkbox"
                        id="consent-checkbox"
                        label="I consent to sharing this data for research purposes."
                        checked={consented}
                        onChange={handleConsentChange}
                    />
                </FormGroup>

                <div className="d-flex justify-content-between">
                    <Button
                        color="outline-secondary"
                        onClick={() => setCurrentStep(1)}
                    >
                        Back
                    </Button>
                    <Button
                        color="primary"
                        onClick={handleProceedToSurvey}
                        disabled={!consented}
                        style={{
                            background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)',
                            border: 'none'
                        }}
                    >
                        Continue to Survey
                    </Button>
                </div>
            </CardBody>
        </Card>
    );

    const renderStep4EmailAndSubmit = () => (
        <Card className="shadow-sm" style={{ borderColor: '#00D1FF', borderWidth: 2 }}>
            <CardBody>
                <CardTitle tag="h5" className="mb-3">
                    <FaEnvelope className="mr-2" />
                    Final Step
                </CardTitle>

                <Alert color="success" className="mb-4">
                    <FaCheck className="mr-2" />
                    {surveyCompleted ? 'Survey completed!' : surveySkipped ? 'No survey available - continuing to submission.' : 'Survey completed!'}
                </Alert>

                <FormGroup className="mb-3">
                    <Label for="email-input">
                        <FaEnvelope className="mr-1" />
                        Email for revenue share (optional)
                    </Label>
                    <Input
                        type="email"
                        id="email-input"
                        placeholder="your@email.com"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                    />
                    <small className="text-muted">
                        Enter your email to receive a share of revenue when your data is sold to researchers.
                    </small>
                </FormGroup>

                {submitError && <Alert color="danger" className="mb-3">{submitError}</Alert>}

                <div className="d-flex justify-content-between">
                    <Button
                        color="outline-secondary"
                        onClick={() => setCurrentStep(3)}
                    >
                        Back
                    </Button>
                    <Button
                        color="primary"
                        block
                        onClick={handleSubmit}
                        disabled={submitting}
                        style={{
                            background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)',
                            border: 'none'
                        }}
                    >
                        {submitting ? <Spinner size="sm" /> : 'Share My Anonymized Data'}
                    </Button>
                </div>
            </CardBody>
        </Card>
    );

    return (
        <Container className="py-5" style={{ minHeight: '80vh' }}>
            <Row className="justify-content-center">
                <Col md="10" lg="8">
                    {renderStepIndicator()}

                    {submitted ? (
                        <Card className="shadow-sm" style={{ borderColor: '#28a745', borderWidth: 2 }}>
                            <CardBody className="text-center">
                                <FaCheck size={48} color="#28a745" className="mb-3" />
                                <h4 className="text-success">Thank You!</h4>
                                <p>Your anonymized data has been shared for research purposes.</p>
                                {email && <p className="text-muted">We'll contact you at {email} when your data is licensed.</p>}
                            </CardBody>
                        </Card>
                    ) : (
                        <>
                            {currentStep === 1 && renderStep1Stats()}
                            {currentStep === 2 && renderStep2Consent()}
                            {currentStep === 3 && (
                                <SurveyStep
                                    onComplete={handleSurveyComplete}
                                    onSkip={handleSurveySkip}
                                />
                            )}
                            {currentStep === 4 && renderStep4EmailAndSubmit()}
                        </>
                    )}
                </Col>
            </Row>
        </Container>
    );
};

export default ContributorStatsPage;