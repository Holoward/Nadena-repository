import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, Button, Alert, Form, FormGroup, Label, Input, CustomInput } from 'reactstrap';
import { useAuth } from '../context/AuthContext';
import { FaChevronLeft, FaChevronRight, FaCheck } from 'react-icons/fa';

const SurveyStep = ({ onComplete, onSkip }) => {
    const { auth } = useAuth();
    const [loading, setLoading] = useState(true);
    const [survey, setSurvey] = useState(null);
    const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
    const [responses, setResponses] = useState({});
    const [error, setError] = useState(null);
    const [submitting, setSubmitting] = useState(false);
    const [alreadyCompleted, setAlreadyCompleted] = useState(false);

    useEffect(() => {
        fetchActiveSurvey();
    }, []);

    const fetchActiveSurvey = async () => {
        try {
            const response = await fetch('/api/v1/survey/active');
            if (response.status === 404) {
                onSkip();
                return;
            }
            if (!response.ok) {
                throw new Error('Failed to load survey');
            }
            const data = await response.json();
            setSurvey(data.data || data);
        } catch (err) {
            console.error('Error fetching survey:', err);
            onSkip();
        } finally {
            setLoading(false);
        }
    };

    const handleResponseChange = (questionId, value, isMultiChoice = false) => {
        setResponses(prev => {
            if (isMultiChoice) {
                const current = prev[questionId] || [];
                if (current.includes(value)) {
                    return { ...prev, [questionId]: current.filter(v => v !== value) };
                } else {
                    return { ...prev, [questionId]: [...current, value] };
                }
            } else {
                return { ...prev, [questionId]: value };
            }
        });
    };

    const isCurrentQuestionAnswered = () => {
        if (!survey || !survey.questions) return false;
        const question = survey.questions[currentQuestionIndex];
        if (!question) return false;
        
        const response = responses[question.id];
        
        if (question.questionType === 'MultiChoice') {
            return response && Array.isArray(response) && response.length > 0;
        }
        
        return response && typeof response === 'string' && response.trim() !== '';
    };

    const validateAllRequired = () => {
        if (!survey || !survey.questions) return true;
        
        for (const question of survey.questions) {
            if (question.isRequired) {
                const response = responses[question.id];
                if (question.questionType === 'MultiChoice') {
                    if (!response || !Array.isArray(response) || response.length === 0) {
                        return false;
                    }
                } else {
                    if (!response || typeof response !== 'string' || response.trim() === '') {
                        return false;
                    }
                }
            }
        }
        return true;
    };

    const canProceed = () => {
        return isCurrentQuestionAnswered();
    };

    const handleNext = () => {
        if (currentQuestionIndex < survey.questions.length - 1) {
            setCurrentQuestionIndex(prev => prev + 1);
        }
    };

    const handleBack = () => {
        if (currentQuestionIndex > 0) {
            setCurrentQuestionIndex(prev => prev - 1);
        }
    };

    const handleSubmit = async () => {
        if (!validateAllRequired()) {
            setError('Please answer all required questions before submitting.');
            return;
        }

        setSubmitting(true);
        setError(null);

        try {
            const formattedResponses = Object.entries(responses).map(([questionId, value]) => ({
                questionId: parseInt(questionId),
                responseValue: Array.isArray(value) ? JSON.stringify(value) : value
            }));

            const response = await fetch('/api/v1/survey/respond', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${auth.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    surveyTemplateId: survey.id,
                    responses: formattedResponses
                })
            });

            if (response.status === 409) {
                setAlreadyCompleted(true);
                setTimeout(() => onSkip(), 2000);
                return;
            }

            if (!response.ok) {
                const data = await response.json();
                throw new Error(data.message || 'Failed to submit survey');
            }

            onComplete();
        } catch (err) {
            setError(err.message || 'An error occurred. Please try again.');
        } finally {
            setSubmitting(false);
        }
    };

    const renderQuestion = (question) => {
        const questionTypes = {
            'SingleChoice': 'radio',
            'MultiChoice': 'checkbox',
            'Scale': 'scale',
            'FreeText': 'freetext'
        };
        
        const type = questionTypes[question.questionType] || question.questionType;
        
        if (type === 'freetext') {
            return (
                <Input
                    type="textarea"
                    rows={4}
                    maxLength={500}
                    value={responses[question.id] || ''}
                    onChange={(e) => handleResponseChange(question.id, e.target.value)}
                    placeholder="Enter your response..."
                />
            );
        }

        if (type === 'scale') {
            const min = question.scaleMin || 1;
            const max = question.scaleMax || 7;
            const options = Array.from({ length: max - min + 1 }, (_, i) => min + i);
            
            return (
                <div>
                    <div className="d-flex justify-content-between mb-2">
                        <span className="text-muted">{question.scaleMinLabel || ''}</span>
                        <span className="text-muted">{question.scaleMaxLabel || ''}</span>
                    </div>
                    <div className="d-flex justify-content-between">
                        {options.map(value => (
                            <Button
                                key={value}
                                color={responses[question.id] === value.toString() ? 'primary' : 'outline-secondary'}
                                size="sm"
                                onClick={() => handleResponseChange(question.id, value.toString())}
                                style={{ minWidth: '40px' }}
                            >
                                {value}
                            </Button>
                        ))}
                    </div>
                </div>
            );
        }

        let options = [];
        try {
            options = question.options ? JSON.parse(question.options) : [];
        } catch {
            options = [];
        }

        const isMultiChoice = type === 'checkbox';

        return (
            <Form>
                {options.map(option => (
                    <FormGroup key={option} check>
                        {isMultiChoice ? (
                            <CustomInput
                                type="checkbox"
                                id={`${question.id}-${option}`}
                                label={option}
                                checked={(responses[question.id] || []).includes(option)}
                                onChange={() => handleResponseChange(question.id, option, true)}
                            />
                        ) : (
                            <CustomInput
                                type="radio"
                                name={`question-${question.id}`}
                                id={`${question.id}-${option}`}
                                label={option}
                                checked={responses[question.id] === option}
                                onChange={() => handleResponseChange(question.id, option)}
                            />
                        )}
                    </FormGroup>
                ))}
            </Form>
        );
    };

    if (loading) {
        return (
            <Container className="py-5">
                <Row className="justify-content-center">
                    <Col md="10" lg="8" className="text-center">
                        <p>Loading survey...</p>
                    </Col>
                </Row>
            </Container>
        );
    }

    if (alreadyCompleted) {
        return (
            <Container className="py-5">
                <Row className="justify-content-center">
                    <Col md="10" lg="8">
                        <Alert color="success">
                            <FaCheck className="mr-2" />
                            You have already completed this survey. Continuing to next step.
                        </Alert>
                    </Col>
                </Row>
            </Container>
        );
    }

    if (!survey || !survey.questions || survey.questions.length === 0) {
        return null;
    }

    const currentQuestion = survey.questions[currentQuestionIndex];
    const totalQuestions = survey.questions.length;

    return (
        <Container className="py-4">
            <Row className="justify-content-center">
                <Col md="10" lg="8">
                    <Card className="shadow-sm" style={{ borderColor: '#00D1FF', borderWidth: 2 }}>
                        <CardBody>
                            <div className="text-center mb-4">
                                <h4 style={{ color: '#00D1FF' }}>{survey.title}</h4>
                                <p className="text-muted">{survey.description}</p>
                            </div>

                            <Alert color="info" className="mb-4">
                                Question {currentQuestionIndex + 1} of {totalQuestions}
                            </Alert>

                            {error && <Alert color="danger" className="mb-3">{error}</Alert>}

                            <FormGroup>
                                <Label className="font-weight-bold">
                                    {currentQuestion.questionText}
                                    {currentQuestion.isRequired && <span className="text-danger ml-1">*</span>}
                                </Label>
                                {renderQuestion(currentQuestion)}
                            </FormGroup>

                            <div className="d-flex justify-content-between mt-4">
                                <Button
                                    color="outline-secondary"
                                    onClick={handleBack}
                                    disabled={currentQuestionIndex === 0}
                                >
                                    <FaChevronLeft className="mr-1" /> Back
                                </Button>

                                {currentQuestionIndex === totalQuestions - 1 ? (
                                    <Button
                                        color="primary"
                                        onClick={handleSubmit}
                                        disabled={submitting || !validateAllRequired()}
                                        style={{
                                            background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)',
                                            border: 'none'
                                        }}
                                    >
                                        {submitting ? 'Submitting...' : 'Submit Survey'}
                                    </Button>
                                ) : (
                                    <Button
                                        color="primary"
                                        onClick={handleNext}
                                        disabled={!canProceed()}
                                        style={{
                                            background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)',
                                            border: 'none'
                                        }}
                                    >
                                        Next <FaChevronRight className="ml-1" />
                                    </Button>
                                )}
                            </div>
                        </CardBody>
                    </Card>
                </Col>
            </Row>
        </Container>
    );
};

export default SurveyStep;