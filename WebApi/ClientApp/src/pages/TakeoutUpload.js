import React, { useState } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, Button, Alert, Spinner } from 'reactstrap';
import { useHistory } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { FaGoogle, FaYoutube, FaUpload, FaInfoCircle, FaCheck, FaChartLine } from 'react-icons/fa';

const categorizeUploadError = (error, responseStatus) => {
    // Server unreachable (network error)
    if (error && (error.includes('Failed to fetch') || error.includes('Network error') || error.includes('network'))) {
        return {
            message: "We couldn't reach the Nadena server. Please check your internet connection and try again.",
            type: 'network'
        };
    }
    
    // Auth expired
    if (responseStatus === 401 || (error && (error.toLowerCase().includes('expired') || error.toLowerCase().includes('unauthorized') || error.toLowerCase().includes('authenticated')))) {
        return {
            message: "Your session has expired. Please log in again.",
            type: 'auth',
            redirect: true
        };
    }
    
    // Backend error messages mapped to user-friendly messages
    const errorMappings = [
        {
            patterns: ['watch-history.json was not found', 'not found in the uploaded', 'not found'],
            message: "We couldn't find your watch history in this file. Make sure you selected 'YouTube and YouTube Music' when creating your Google Takeout export."
        },
        {
            patterns: ['did not contain any records', 'empty', 'no records'],
            message: "Your watch history appears to be empty. This can happen if you've cleared your YouTube history. You need at least some watch history to contribute."
        },
        {
            patterns: ['not a valid ZIP', 'invalid data', 'invalid zip', 'not a valid'],
            message: "This doesn't look like a Google Takeout ZIP file. Please download your data directly from takeout.google.com."
        },
        {
            patterns: ['no valid YouTube watch events', 'no valid', 'could not be parsed'],
            message: "We couldn't find any valid watch events in your file. Make sure you exported watch history."
        }
    ];
    
    if (error) {
        for (const mapping of errorMappings) {
            if (mapping.patterns.some(p => error.toLowerCase().includes(p.toLowerCase()))) {
                return { message: mapping.message };
            }
        }
    }
    
    // Default error
    return { message: error || 'An unexpected error occurred. Please try again.' };
};

const TakeoutUploadPage = () => {
    const history = useHistory();
    const { auth } = useAuth();
    const [file, setFile] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);
    const [stats, setStats] = useState(null);

    const userId = auth.user?.id || auth.user?.sub;

    const handleFileChange = (e) => {
        const selectedFile = e.target.files[0];
        if (selectedFile) {
            if (!selectedFile.name.endsWith('.zip')) {
                setError('Only ZIP files are accepted. Please upload a Google Takeout ZIP file.');
                return;
            }
            if (selectedFile.size > 500 * 1024 * 1024) {
                setError('Your file is too large to upload directly. Please contact david@nadena.tech and we\'ll arrange an alternative upload.');
                return;
            }
            setFile(selectedFile);
            setError(null);
        }
    };

    const handleUpload = async () => {
        if (!file) {
            setError('Please select a file to upload.');
            return;
        }

        setLoading(true);
        setError(null);

        let responseStatus = null;

        try {
            const formData = new FormData();
            formData.append('zipFile', file);

            const response = await fetch('/api/v1/takeout/upload', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${auth.token}`
                },
                body: formData
            });

            responseStatus = response.status;
            const data = await response.json();

            if (!response.ok) {
                const categorizedError = categorizeUploadError(data.message, response.status);
                
                if (categorizedError.redirect) {
                    setError(categorizedError.message);
                    setTimeout(() => {
                        history.push('/login');
                    }, 3000);
                    setLoading(false);
                    return;
                } else {
                    throw new Error(categorizedError.message);
                }
            }

            // Now fetch the stats
            const statsResponse = await fetch(`/api/v1/takeout/stats/${userId}`, {
                headers: { 'Authorization': `Bearer ${auth.token}` }
            });

            const statsData = await statsResponse.json();

            if (!statsResponse.ok) {
                // Even if stats fail, show success since upload worked
                setSuccess(true);
                setLoading(false);
                return;
            }

            setStats(statsData.data);
            setSuccess(true);
        } catch (err) {
            const errorMessage = err.message || 'An error occurred. Please try again.';
            setError(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleContinue = () => {
        if (stats) {
            sessionStorage.setItem('contributorStats', JSON.stringify(stats));
        }
        history.push('/stats');
    };

    return (
        <Container className="py-5" style={{ minHeight: '80vh' }}>
            <Row className="justify-content-center">
                <Col md="10" lg="8">
                    <div className="text-center mb-5">
                        <h2 className="font-weight-bold" style={{ color: '#00D1FF' }}>
                            Upload Your YouTube Watch History
                        </h2>
                        <p className="text-muted">
                            Help researchers understand how you watch YouTube by uploading your Google Takeout data.
                        </p>
                    </div>

                    <Card className="mb-4 shadow-sm">
                        <CardBody>
                            <CardTitle tag="h5" className="mb-3">
                                <FaGoogle className="mr-2" />
                                What is Google Takeout?
                            </CardTitle>
                            <p style={{ color: '#555' }}>
                                Google Takeout is a service that lets you download a copy of all your data stored in your Google account,
                                including your YouTube watch history.
                            </p>
                            <a
                                href="https://takeout.google.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="btn btn-outline-primary btn-sm"
                            >
                                <FaYoutube className="mr-1" />
                                Go to takeout.google.com
                            </a>
                        </CardBody>
                    </Card>

                    <Card className="mb-4 shadow-sm">
                        <CardBody>
                            <CardTitle tag="h5" className="mb-3">
                                Step-by-Step Instructions
                            </CardTitle>
                            <ol style={{ color: '#555', lineHeight: '1.8' }}>
                                <li className="mb-2">
                                    Go to <strong>takeout.google.com</strong> and sign in with your Google account
                                </li>
                                <li className="mb-2">
                                    Click <strong>"Deselect all"</strong> at the top, then find and select only <strong>"YouTube"</strong> and <strong>"YouTube Music"</strong> (or "YouTube and YouTube Music")
                                </li>
                                <li className="mb-2">
                                    Under <strong>"Multiple formats"</strong> (or "File delivery method"), change the format for <strong>"Watch history"</strong> from HTML to <strong>JSON</strong>
                                </li>
                                <li className="mb-2">
                                    Click <strong>"Next step"</strong> and then <strong>"Create export"</strong>
                                </li>
                                <li className="mb-2">
                                    Wait for the email notification, then <strong>download the ZIP file</strong>
                                </li>
                                <li className="mb-2">
                                    Upload that ZIP file below
                                </li>
                            </ol>
                        </CardBody>
                    </Card>

                    {error && (
                        <Alert color="danger" className="mb-4">
                            {error}
                        </Alert>
                    )}

                    {success && stats ? (
                        <Card className="mb-4 shadow-sm" style={{ borderColor: '#28a745', borderWidth: 2 }}>
                            <CardBody>
                                <div className="text-center mb-4">
                                    <FaCheck size={40} color="#28a745" />
                                    <h4 className="mt-3 text-success">Upload Successful!</h4>
                                </div>
                                <Row className="text-center">
                                    <Col md="4" className="mb-3">
                                        <div style={{ fontSize: 24, fontWeight: 700 }}>{stats.data?.totalVideos?.toLocaleString() || 0}</div>
                                        <div className="text-muted">Total Videos</div>
                                    </Col>
                                    <Col md="4" className="mb-3">
                                        <div style={{ fontSize: 24, fontWeight: 700 }}>{stats.data?.uniqueChannels?.toLocaleString() || 0}</div>
                                        <div className="text-muted">Unique Channels</div>
                                    </Col>
                                    <Col md="4" className="mb-3">
                                        <div style={{ fontSize: 24, fontWeight: 700 }}>{stats.data?.historyDays || 0}</div>
                                        <div className="text-muted">Days of History</div>
                                    </Col>
                                </Row>
                                <Alert color="info" className="mt-3">
                                    <FaChartLine className="mr-2" />
                                    Here is what will be shared if you consent.
                                </Alert>
                                <Button
                                    color="success"
                                    block
                                    size="lg"
                                    onClick={handleContinue}
                                    style={{ background: 'linear-gradient(90deg, #28a745 0%, #20c997 100%)', border: 'none' }}
                                >
                                    Continue to Preview
                                </Button>
                            </CardBody>
                        </Card>
                    ) : (
                        <Card className="shadow-sm" style={{ borderColor: '#00D1FF', borderWidth: 2 }}>
                            <CardBody>
                                <div className="text-center mb-4">
                                    <FaUpload size={40} color="#00D1FF" />
                                </div>

                                <div className="text-center mb-4">
                                    <input
                                        type="file"
                                        accept=".zip"
                                        onChange={handleFileChange}
                                        id="zip-file-input"
                                        style={{ display: 'none' }}
                                    />
                                    <label htmlFor="zip-file-input" className="btn btn-outline-primary btn-lg">
                                        {file ? file.name : 'Select ZIP File'}
                                    </label>
                                    {file && (
                                        <div className="mt-2 text-muted">
                                            {(file.size / 1024 / 1024).toFixed(2)} MB
                                        </div>
                                    )}
                                </div>

                                <Button
                                    color="primary"
                                    block
                                    size="lg"
                                    onClick={handleUpload}
                                    disabled={loading || !file}
                                    style={{
                                        background: 'linear-gradient(90deg, #00D1FF 0%, #0033FF 100%)',
                                        border: 'none'
                                    }}
                                >
                                    {loading ? <Spinner size="sm" /> : 'Upload and Process'}
                                </Button>

                                <Alert color="info" className="mt-3" style={{ fontSize: 14 }}>
                                    <FaInfoCircle className="mr-2" />
                                    Your data is processed locally. We extract only anonymized statistics -
                                    no video titles, channel names, or URLs are stored.
                                </Alert>
                            </CardBody>
                        </Card>
                    )}
                </Col>
            </Row>
        </Container>
    );
};

export default TakeoutUploadPage;