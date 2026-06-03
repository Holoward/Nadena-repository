import React, { useState, useEffect } from 'react';
import {
    View,
    Text,
    StyleSheet,
    ActivityIndicator,
    Alert,
    ScrollView,
    RefreshControl,
} from 'react-native';
import { jwtDecode } from 'jwt-decode';
import { getVolunteerByUserId, getToken } from '../api/client';

const VolunteerDashboardScreen = ({ navigation }) => {
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [volunteerData, setVolunteerData] = useState(null);
    const [error, setError] = useState(null);

    const fetchVolunteerData = async () => {
        try {
            const token = await getToken();
            if (!token) {
                navigation.replace('Login');
                return;
            }

            const decoded = jwtDecode(token);
            const userId = decoded.sub || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

            const data = await getVolunteerByUserId(userId);
            setVolunteerData(data);
            setError(null);
        } catch (err) {
            console.error('Error fetching volunteer data:', err);
            setError('Failed to load volunteer data');
            if (err.response?.status === 401) {
                navigation.replace('Login');
            }
        } finally {
            setLoading(false);
            setRefreshing(false);
        }
    };

    useEffect(() => {
        fetchVolunteerData();
    }, []);

    const onRefresh = () => {
        setRefreshing(true);
        fetchVolunteerData();
    };

    const getStatusSteps = () => {
        const status = volunteerData?.status?.toLowerCase() || 'registered';
        return {
            registered: true,
            activated: status === 'activated' || status === 'filereceived' || status === 'paid',
            fileReceived: status === 'filereceived' || status === 'paid',
            paid: status === 'paid',
        };
    };

    const getCurrentStep = () => {
        const status = volunteerData?.status?.toLowerCase() || 'registered';
        if (status === 'paid') return 3;
        if (status === 'filereceived') return 2;
        if (status === 'activated') return 1;
        return 0;
    };

    const getContextPanel = () => {
        const status = volunteerData?.status?.toLowerCase() || 'registered';
        switch (status) {
            case 'registered':
                return {
                    title: 'Waiting for Activation',
                    message: 'We will notify you when a buyer confirms your dataset purchase and your profile is activated.',
                    actionText: null,
                };
            case 'activated':
                return {
                    title: 'Ready to Upload',
                    instructions: [
                        '1. Go to takeout.google.com and sign in',
                        '2. Click "Deselect all", then select only "YouTube and YouTube Music"',
                        '3. Choose "Export once" and set format to .zip',
                        '4. Click "Create export" and wait for the download link',
                        '5. Download the ZIP file and upload it below',
                    ],
                    actionText: 'Go to Upload',
                };
            case 'filereceived':
                return {
                    title: 'File Received',
                    message: 'Your file has been received and is being processed. Payment is being processed and will be sent to your PayPal account.',
                    actionText: null,
                };
            case 'paid':
                return {
                    title: 'Payment Complete',
                    message: `Your payment has been processed. Amount: $${volunteerData?.dataEstimatedValue?.toFixed(2) || '0.00'}. Thank you for contributing your data!`,
                    actionText: null,
                };
            default:
                return {
                    title: 'Status Unknown',
                    message: 'Please contact support.',
                    actionText: null,
                };
        }
    };

    if (loading) {
        return (
            <View style={styles.centerContainer}>
                <ActivityIndicator size="large" color="#1a2f4a" />
                <Text style={styles.loadingText}>Loading...</Text>
            </View>
        );
    }

    if (error) {
        return (
            <View style={styles.centerContainer}>
                <Text style={styles.errorText}>{error}</Text>
            </View>
        );
    }

    const steps = getStatusSteps();
    const currentStep = getCurrentStep();
    const context = getContextPanel();

    return (
        <ScrollView
            style={styles.container}
            refreshControl={
                <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
            }
        >
            <View style={styles.content}>
                <Text style={styles.title}>Volunteer Dashboard</Text>

                <View style={styles.statusCard}>
                    <Text style={styles.statusTitle}>Application Status</Text>
                    <Text style={styles.statusText}>{volunteerData?.status || 'Registered'}</Text>
                </View>

                <View style={styles.trackerContainer}>
                    <View style={styles.step}>
                        <View style={[
                            styles.stepCircle,
                            currentStep >= 0 && styles.stepCircleCompleted,
                            currentStep === 0 && styles.stepCircleActive,
                        ]}>
                            {currentStep > 0 ? (
                                <Text style={styles.stepCheckmark}>✓</Text>
                            ) : (
                                <Text style={[styles.stepNumber, currentStep === 0 && styles.stepNumberActive]}>1</Text>
                            )}
                        </View>
                        <Text style={[styles.stepLabel, currentStep >= 0 && styles.stepLabelActive]}>Registered</Text>
                    </View>

                    <View style={[styles.connector, currentStep >= 1 && styles.connectorActive]} />

                    <View style={styles.step}>
                        <View style={[
                            styles.stepCircle,
                            currentStep >= 1 && styles.stepCircleCompleted,
                            currentStep === 1 && styles.stepCircleActive,
                        ]}>
                            {currentStep > 1 ? (
                                <Text style={styles.stepCheckmark}>✓</Text>
                            ) : (
                                <Text style={[styles.stepNumber, currentStep === 1 && styles.stepNumberActive]}>2</Text>
                            )}
                        </View>
                        <Text style={[styles.stepLabel, currentStep >= 1 && styles.stepLabelActive]}>Activated</Text>
                    </View>

                    <View style={[styles.connector, currentStep >= 2 && styles.connectorActive]} />

                    <View style={styles.step}>
                        <View style={[
                            styles.stepCircle,
                            currentStep >= 2 && styles.stepCircleCompleted,
                            currentStep === 2 && styles.stepCircleActive,
                        ]}>
                            {currentStep > 2 ? (
                                <Text style={styles.stepCheckmark}>✓</Text>
                            ) : (
                                <Text style={[styles.stepNumber, currentStep === 2 && styles.stepNumberActive]}>3</Text>
                            )}
                        </View>
                        <Text style={[styles.stepLabel, currentStep >= 2 && styles.stepLabelActive]}>File Received</Text>
                    </View>

                    <View style={[styles.connector, currentStep >= 3 && styles.connectorActive]} />

                    <View style={styles.step}>
                        <View style={[
                            styles.stepCircle,
                            currentStep >= 3 && styles.stepCircleCompleted,
                            currentStep === 3 && styles.stepCircleActive,
                        ]}>
                            <Text style={[styles.stepNumber, currentStep === 3 && styles.stepNumberActive]}>4</Text>
                        </View>
                        <Text style={[styles.stepLabel, currentStep >= 3 && styles.stepLabelActive]}>Paid</Text>
                    </View>
                </View>

                <View style={styles.contextPanel}>
                    <Text style={styles.contextTitle}>{context.title}</Text>
                    {context.message && (
                        <Text style={styles.contextMessage}>{context.message}</Text>
                    )}
                    {context.instructions && context.instructions.map((step, index) => (
                        <Text key={index} style={styles.instructionStep}>{step}</Text>
                    ))}
                    {context.actionText && (
                        <TouchableOpacity
                            style={styles.contextActionButton}
                            onPress={() => navigation.navigate('Upload')}
                        >
                            <Text style={styles.contextActionText}>{context.actionText}</Text>
                        </TouchableOpacity>
                    )}
                </View>

                {volunteerData && (
                    <View style={styles.infoCard}>
                        <Text style={styles.infoTitle}>Volunteer Information</Text>
                        <View style={styles.infoRow}>
                            <Text style={styles.infoLabel}>Status:</Text>
                            <Text style={styles.infoValue}>{volunteerData.status}</Text>
                        </View>
                        {volunteerData.dataSourceType && (
                            <View style={styles.infoRow}>
                                <Text style={styles.infoLabel}>Data Source:</Text>
                                <Text style={styles.infoValue}>{volunteerData.dataSourceType}</Text>
                            </View>
                        )}
                        {volunteerData.payPalEmail && (
                            <View style={styles.infoRow}>
                                <Text style={styles.infoLabel}>PayPal Email:</Text>
                                <Text style={styles.infoValue}>{volunteerData.payPalEmail}</Text>
                            </View>
                        )}
                        {volunteerData.dataEstimatedValue > 0 && (
                            <View style={styles.infoRow}>
                                <Text style={styles.infoLabel}>Estimated Value:</Text>
                                <Text style={styles.infoValue}>${volunteerData.dataEstimatedValue?.toFixed(2)}</Text>
                            </View>
                        )}
                    </View>
                )}
            </View>
        </ScrollView>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
    },
    centerContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#f5f5f5',
    },
    content: {
        padding: 20,
    },
    title: {
        fontSize: 24,
        fontWeight: 'bold',
        color: '#1a2f4a',
        marginBottom: 20,
    },
    loadingText: {
        marginTop: 10,
        fontSize: 16,
        color: '#666',
    },
    errorText: {
        fontSize: 16,
        color: '#ff3b30',
    },
    statusCard: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 20,
        marginBottom: 20,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    statusTitle: {
        fontSize: 18,
        fontWeight: '600',
        color: '#1a2f4a',
        marginBottom: 10,
    },
    statusText: {
        fontSize: 14,
        color: '#666',
        lineHeight: 20,
    },
    trackerContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 20,
        marginBottom: 20,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    step: {
        alignItems: 'center',
        width: 60,
    },
    stepCircle: {
        width: 36,
        height: 36,
        borderRadius: 18,
        backgroundColor: '#ddd',
        justifyContent: 'center',
        alignItems: 'center',
        marginBottom: 8,
    },
    stepCircleActive: {
        backgroundColor: '#1a2f4a',
    },
    stepCircleCompleted: {
        backgroundColor: '#34C759',
    },
    stepNumber: {
        fontSize: 16,
        fontWeight: 'bold',
        color: '#666',
    },
    stepNumberActive: {
        color: '#fff',
    },
    stepCheckmark: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#fff',
    },
    stepLabel: {
        fontSize: 11,
        color: '#999',
        textAlign: 'center',
    },
    stepLabelActive: {
        color: '#1a2f4a',
        fontWeight: '600',
    },
    connector: {
        width: 30,
        height: 2,
        backgroundColor: '#ddd',
    },
    connectorActive: {
        backgroundColor: '#1a2f4a',
    },
    contextPanel: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 20,
        marginBottom: 20,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    contextTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: '#1a2f4a',
        marginBottom: 10,
    },
    contextMessage: {
        fontSize: 14,
        color: '#666',
        lineHeight: 20,
    },
    instructionStep: {
        fontSize: 14,
        color: '#333',
        lineHeight: 24,
    },
    contextActionButton: {
        backgroundColor: '#1a2f4a',
        paddingVertical: 12,
        paddingHorizontal: 20,
        borderRadius: 8,
        alignItems: 'center',
        marginTop: 16,
    },
    contextActionText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    infoCard: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 20,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    infoTitle: {
        fontSize: 18,
        fontWeight: '600',
        color: '#1a2f4a',
        marginBottom: 15,
    },
    infoRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 10,
    },
    infoLabel: {
        fontSize: 14,
        color: '#666',
    },
    infoValue: {
        fontSize: 14,
        fontWeight: '600',
        color: '#333',
    },
});

export default VolunteerDashboardScreen;
