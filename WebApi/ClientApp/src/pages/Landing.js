import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Container, Row, Col, Button, Collapse } from 'reactstrap';
import { useAuth } from '../context/AuthContext';
import { FaShieldAlt, FaUserSecret, FaTrashAlt, FaQuestionCircle, FaYoutube, FaEye, FaHandshake } from 'react-icons/fa';

const Landing = () => {
    const { auth } = useAuth();
    
    const primaryColor = '#00D1FF';
    const darkBg = '#0a1628';

    const [faqOpen, setFaqOpen] = useState({});

    const toggleFaq = (index) => {
        setFaqOpen(prev => ({ ...prev, [index]: !prev[index] }));
    };

    const faqs = [
        {
            question: "What data do you collect?",
            answer: "We collect anonymized YouTube viewing patterns including total videos watched, when you watch (time of day, day of week), and content category preferences. We NEVER collect video titles, channel names, URLs, or any personally identifiable information."
        },
        {
            question: "Who buys this data?",
            answer: "Academic researchers, market analysts, and data science companies purchase anonymized datasets for studying YouTube consumption patterns. All buyers are verified and must agree to our terms of use."
        },
        {
            question: "How is my data anonymised?",
            answer: "Before any data is shared, we hash all video IDs and channel IDs using SHA-256. Your name, email, and any direct identifiers are completely removed. Only aggregate viewing patterns remain."
        },
        {
            question: "How do I get credit?",
            answer: "When your anonymized data is licensed to a buyer, you receive a revenue share via PayPal. The more quality data you contribute, the higher your potential earnings."
        },
        {
            question: "Can I delete my data later?",
            answer: "Yes! Under GDPR, you can request complete deletion of your data at any time. Visit your dashboard settings or contact privacy@nadena.com to delete all your data."
        }
    ];

    return (
        <div style={{ backgroundColor: darkBg, minHeight: '100vh', color: '#fff' }}>
            {/* Navigation */}
            <nav className="navbar navbar-expand-lg" style={{ backgroundColor: 'rgba(10, 22, 40, 0.95)', padding: '15px 0', position: 'sticky', top: 0, zIndex: 1000 }}>
                <Container>
                    <Row className="align-items-center justify-content-between" style={{ width: '100%', margin: 0 }}>
                        <Col xs="auto">
                            <Link to="/" style={{ textDecoration: 'none' }}>
                                <span style={{ fontSize: '1.5rem', fontWeight: 'bold', color: primaryColor }}>NADENA</span>
                            </Link>
                        </Col>
                        <Col xs="auto" className="d-flex gap-3">
                            <Link to="/login" className="btn" style={{ color: '#fff', border: 'none', background: 'transparent' }}>Log In</Link>
                            <Link to="/register" className="btn" style={{ backgroundColor: primaryColor, color: '#0a1628', fontWeight: '600', borderRadius: '6px' }}>Get Started</Link>
                        </Col>
                    </Row>
                </Container>
            </nav>

            {/* Hero Section */}
            <section style={{ padding: '100px 0 80px', textAlign: 'center' }}>
                <Container>
                    <Row className="justify-content-center">
                        <Col md="10" lg="8">
                            <h1 style={{ fontSize: 'clamp(2rem, 5vw, 3.5rem)', fontWeight: 'bold', marginBottom: '24px', lineHeight: '1.2' }}>
                                Get paid for your YouTube data — on your terms
                            </h1>
                            <p style={{ fontSize: '1.25rem', marginBottom: '40px', opacity: 0.9, maxWidth: '700px', margin: '0 auto 40px', color: '#a0aec0' }}>
                                Nadena collects your viewing history with your explicit consent and sells it as anonymised research datasets. You keep control. You get credit.
                            </p>
                            <div className="d-flex gap-3 justify-content-center flex-wrap">
                                <Link to="/register">
                                    <Button
                                        size="lg"
                                        style={{
                                            backgroundColor: primaryColor,
                                            color: '#0a1628',
                                            border: 'none',
                                            fontWeight: '600',
                                            padding: '14px 36px',
                                            borderRadius: '6px'
                                        }}
                                    >
                                        Contribute Data
                                    </Button>
                                </Link>
                                <Link to="/marketplace">
                                    <Button
                                        size="lg"
                                        outline
                                        style={{
                                            backgroundColor: 'transparent',
                                            color: '#fff',
                                            border: '2px solid rgba(255,255,255,0.3)',
                                            fontWeight: '600',
                                            padding: '12px 34px',
                                            borderRadius: '6px'
                                        }}
                                    >
                                        Browse Datasets
                                    </Button>
                                </Link>
                            </div>
                        </Col>
                    </Row>
                </Container>
            </section>

            {/* How It Works */}
            <section style={{ padding: '80px 0', backgroundColor: 'rgba(255,255,255,0.03)' }}>
                <Container>
                    <Row className="justify-content-center text-center mb-5">
                        <Col md="8">
                            <h2 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '16px' }}>
                                How It Works
                            </h2>
                        </Col>
                    </Row>
                    <Row className="text-center">
                        <Col md="4" className="mb-4 mb-md-0">
                            <div style={{
                                backgroundColor: 'rgba(0, 209, 255, 0.1)',
                                width: '80px',
                                height: '80px',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                margin: '0 auto 20px',
                                fontSize: '1.75rem',
                                color: primaryColor
                            }}>
                                <FaYoutube />
                            </div>
                            <h4 style={{ fontWeight: '600', marginBottom: '12px' }}>Step 1: Connect</h4>
                            <p style={{ color: '#a0aec0', fontSize: '0.95rem' }}>
                                Export your YouTube data via Google Takeout and upload it to Nadena
                            </p>
                        </Col>
                        <Col md="4" className="mb-4 mb-md-0">
                            <div style={{
                                backgroundColor: 'rgba(0, 209, 255, 0.1)',
                                width: '80px',
                                height: '80px',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                margin: '0 auto 20px',
                                fontSize: '1.75rem',
                                color: primaryColor
                            }}>
                                <FaEye />
                            </div>
                            <h4 style={{ fontWeight: '600', marginBottom: '12px' }}>Step 2: Review</h4>
                            <p style={{ color: '#a0aec0', fontSize: '0.95rem' }}>
                                See exactly what will be shared before you decide — every data point is visible
                            </p>
                        </Col>
                        <Col md="4">
                            <div style={{
                                backgroundColor: 'rgba(0, 209, 255, 0.1)',
                                width: '80px',
                                height: '80px',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                margin: '0 auto 20px',
                                fontSize: '1.75rem',
                                color: primaryColor
                            }}>
                                <FaHandshake />
                            </div>
                            <h4 style={{ fontWeight: '600', marginBottom: '12px' }}>Step 3: Consent</h4>
                            <p style={{ color: '#a0aec0', fontSize: '0.95rem' }}>
                                Agree to share. Withdraw your consent anytime — your data, your rules
                            </p>
                        </Col>
                    </Row>
                </Container>
            </section>

            {/* Trust Section */}
            <section style={{ padding: '80px 0', backgroundColor: darkBg }}>
                <Container>
                    <Row className="justify-content-center text-center mb-5">
                        <Col md="8">
                            <h2 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '16px' }}>
                                Why Trust Nadena
                            </h2>
                        </Col>
                    </Row>
                    <Row className="text-center">
                        <Col md="4" className="mb-4 mb-md-0">
                            <div style={{
                                backgroundColor: 'rgba(72, 187, 120, 0.1)',
                                width: '80px',
                                height: '80px',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                margin: '0 auto 20px',
                                fontSize: '1.75rem',
                                color: '#48bb78'
                            }}>
                                <FaShieldAlt />
                            </div>
                            <h4 style={{ fontWeight: '600', marginBottom: '12px' }}>GDPR Compliant</h4>
                            <p style={{ color: '#a0aec0', fontSize: '0.95rem' }}>
                                Fully compliant with EU data protection regulations
                            </p>
                        </Col>
                        <Col md="4" className="mb-4 mb-md-0">
                            <div style={{
                                backgroundColor: 'rgba(66, 153, 225, 0.1)',
                                width: '80px',
                                height: '80px',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                margin: '0 auto 20px',
                                fontSize: '1.75rem',
                                color: '#4299e1'
                            }}>
                                <FaUserSecret />
                            </div>
                            <h4 style={{ fontWeight: '600', marginBottom: '12px' }}>Your Name Never Appears</h4>
                            <p style={{ color: '#a0aec0', fontSize: '0.95rem' }}>
                                All personal identifiers removed before any data is shared
                            </p>
                        </Col>
                        <Col md="4">
                            <div style={{
                                backgroundColor: 'rgba(237, 100, 166, 0.1)',
                                width: '80px',
                                height: '80px',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                margin: '0 auto 20px',
                                fontSize: '1.75rem',
                                color: '#ed64a6'
                            }}>
                                <FaTrashAlt />
                            </div>
                            <h4 style={{ fontWeight: '600', marginBottom: '12px' }}>Delete Anytime</h4>
                            <p style={{ color: '#a0aec0', fontSize: '0.95rem' }}>
                                You can delete your data at any time — no questions asked
                            </p>
                        </Col>
                    </Row>
                </Container>
            </section>

            {/* FAQ Section */}
            <section style={{ padding: '80px 0', backgroundColor: 'rgba(255,255,255,0.03)' }}>
                <Container>
                    <Row className="justify-content-center text-center mb-5">
                        <Col md="8">
                            <h2 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '16px' }}>
                                Frequently Asked Questions
                            </h2>
                        </Col>
                    </Row>
                    <Row className="justify-content-center">
                        <Col md="8">
                            {faqs.map((faq, index) => (
                                <div key={index} style={{ marginBottom: '10px', borderRadius: '8px', overflow: 'hidden', backgroundColor: 'rgba(255,255,255,0.05)' }}>
                                    <button
                                        onClick={() => toggleFaq(index)}
                                        style={{
                                            width: '100%',
                                            padding: '18px 20px',
                                            background: 'transparent',
                                            border: 'none',
                                            color: '#fff',
                                            fontSize: '1rem',
                                            fontWeight: '500',
                                            textAlign: 'left',
                                            display: 'flex',
                                            alignItems: 'center',
                                            justifyContent: 'space-between',
                                            cursor: 'pointer'
                                        }}
                                    >
                                        <span><FaQuestionCircle style={{ marginRight: '12px', color: primaryColor }} />{faq.question}</span>
                                        <span style={{ transform: faqOpen[index] ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform 0.2s' }}>▼</span>
                                    </button>
                                    <Collapse isOpen={faqOpen[index]}>
                                        <div style={{ padding: '0 20px 18px 44px', color: '#a0aec0', fontSize: '0.95rem', lineHeight: '1.6' }}>
                                            {faq.answer}
                                        </div>
                                    </Collapse>
                                </div>
                            ))}
                        </Col>
                    </Row>
                </Container>
            </section>

            {/* Footer */}
            <footer style={{ backgroundColor: '#060d1a', padding: '40px 0 30px', borderTop: '1px solid rgba(255,255,255,0.1)' }}>
                <Container>
                    <Row className="justify-content-between align-items-center">
                        <Col md="6" className="mb-3 mb-md-0">
                            <span style={{ fontSize: '1.25rem', fontWeight: 'bold', color: primaryColor }}>
                                NADENA
                            </span>
                            <p style={{ marginTop: '8px', fontSize: '0.9rem', color: '#718096' }}>
                                Your data. Your consent. Your credit.
                            </p>
                        </Col>
                        <Col md="6" className="text-md-right">
                            <a href="/legal/privacy-policy" style={{ color: '#718096', textDecoration: 'none', marginRight: '24px', fontSize: '0.9rem' }}>Privacy</a>
                            <a href="/legal/terms" style={{ color: '#718096', textDecoration: 'none', fontSize: '0.9rem' }}>Terms</a>
                        </Col>
                    </Row>
                    <hr style={{ borderColor: 'rgba(255,255,255,0.1)', margin: '24px 0 16px' }} />
                    <Row>
                        <Col className="text-center">
                            <p style={{ fontSize: '0.85rem', color: '#4a5568', margin: 0 }}>
                                © {new Date().getFullYear()} Nadena. All rights reserved.
                            </p>
                        </Col>
                    </Row>
                </Container>
            </footer>
        </div>
    );
};

export default Landing;