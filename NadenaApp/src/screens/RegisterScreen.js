import React, { useState } from 'react';
import {
    View,
    Text,
    TextInput,
    TouchableOpacity,
    StyleSheet,
    Alert,
    ActivityIndicator,
    KeyboardAvoidingView,
    Platform,
    ScrollView,
} from 'react-native';
import { jwtDecode } from 'jwt-decode';
import { register } from '../api/client';

const RegisterScreen = ({ navigation }) => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [role, setRole] = useState('Volunteer');
    const [loading, setLoading] = useState(false);

    // Volunteer-specific fields
    const [payPalEmail, setPayPalEmail] = useState('');
    const [youTubeAccountAge, setYouTubeAccountAge] = useState('');
    const [commentCountEstimate, setCommentCountEstimate] = useState('');
    const [contentTypes, setContentTypes] = useState('');

    // Buyer-specific fields
    const [companyName, setCompanyName] = useState('');
    const [useCase, setUseCase] = useState('');

    const handleRegister = async () => {
        if (!email || !password || !firstName || !lastName) {
            Alert.alert('Error', 'Please fill in all required fields');
            return;
        }

        if (password !== confirmPassword) {
            Alert.alert('Error', 'Passwords do not match');
            return;
        }

        if (password.length < 6) {
            Alert.alert('Error', 'Password must be at least 6 characters');
            return;
        }

        setLoading(true);
        try {
            const payload = {
                email,
                password,
                firstName,
                lastName,
                role,
            };

            if (role === 'Volunteer') {
                payload.payPalEmail = payPalEmail;
                payload.youTubeAccountAge = youTubeAccountAge;
                payload.commentCountEstimate = commentCountEstimate;
                payload.contentTypes = contentTypes;
            } else {
                payload.companyName = companyName;
                payload.useCase = useCase;
            }

            const result = await register(email, password, firstName, lastName, role);

            if (result.token) {
                const decoded = jwtDecode(result.token);
                const userRole = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

                if (userRole === 'Volunteer' || role === 'Volunteer') {
                    navigation.replace('VolunteerDashboard');
                } else if (userRole === 'Buyer' || role === 'Buyer') {
                    navigation.replace('BuyerDashboard');
                } else if (userRole === 'Admin') {
                    navigation.replace('AdminDashboard');
                } else {
                    Alert.alert('Error', 'Unknown role');
                }
            }
        } catch (error) {
            console.error('Register error:', error);
            const errorMessage = error.response?.data?.message || 'Registration failed. Please try again.';
            Alert.alert('Error', errorMessage);
        } finally {
            setLoading(false);
        }
    };

    return (
        <KeyboardAvoidingView
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            style={styles.container}
        >
            <ScrollView contentContainerStyle={styles.scrollContent}>
                <View style={styles.formContainer}>
                    <Text style={styles.title}>Nadena</Text>
                    <Text style={styles.subtitle}>Create Account</Text>

                    <View style={styles.inputContainer}>
                        <Text style={styles.label}>First Name</Text>
                        <TextInput
                            style={styles.input}
                            value={firstName}
                            onChangeText={setFirstName}
                            placeholder="Enter your first name"
                            placeholderTextColor="#999"
                            autoCapitalize="words"
                        />
                    </View>

                    <View style={styles.inputContainer}>
                        <Text style={styles.label}>Last Name</Text>
                        <TextInput
                            style={styles.input}
                            value={lastName}
                            onChangeText={setLastName}
                            placeholder="Enter your last name"
                            placeholderTextColor="#999"
                            autoCapitalize="words"
                        />
                    </View>

                    <View style={styles.inputContainer}>
                        <Text style={styles.label}>Email</Text>
                        <TextInput
                            style={styles.input}
                            value={email}
                            onChangeText={setEmail}
                            placeholder="Enter your email"
                            placeholderTextColor="#999"
                            keyboardType="email-address"
                            autoCapitalize="none"
                            autoCorrect={false}
                        />
                    </View>

                    <View style={styles.inputContainer}>
                        <Text style={styles.label}>Password</Text>
                        <TextInput
                            style={styles.input}
                            value={password}
                            onChangeText={setPassword}
                            placeholder="Enter your password"
                            placeholderTextColor="#999"
                            secureTextEntry
                        />
                    </View>

                    <View style={styles.inputContainer}>
                        <Text style={styles.label}>Confirm Password</Text>
                        <TextInput
                            style={styles.input}
                            value={confirmPassword}
                            onChangeText={setConfirmPassword}
                            placeholder="Confirm your password"
                            placeholderTextColor="#999"
                            secureTextEntry
                        />
                    </View>

                    <View style={styles.inputContainer}>
                        <Text style={styles.label}>I am a:</Text>
                        <View style={styles.roleContainer}>
                            <TouchableOpacity
                                style={[
                                    styles.roleButton,
                                    role === 'Volunteer' && styles.roleButtonActive,
                                ]}
                                onPress={() => setRole('Volunteer')}
                            >
                                <Text
                                    style={[
                                        styles.roleButtonText,
                                        role === 'Volunteer' && styles.roleButtonTextActive,
                                    ]}
                                >
                                    Volunteer
                                </Text>
                            </TouchableOpacity>
                            <TouchableOpacity
                                style={[
                                    styles.roleButton,
                                    role === 'Buyer' && styles.roleButtonActive,
                                ]}
                                onPress={() => setRole('Buyer')}
                            >
                                <Text
                                    style={[
                                        styles.roleButtonText,
                                        role === 'Buyer' && styles.roleButtonTextActive,
                                    ]}
                                >
                                    Buyer
                                </Text>
                            </TouchableOpacity>
                        </View>
                    </View>

                    {role === 'Volunteer' && (
                        <>
                            <Text style={styles.sectionHeader}>Volunteer Details</Text>
                            <View style={styles.inputContainer}>
                                <Text style={styles.label}>PayPal Email</Text>
                                <TextInput
                                    style={styles.input}
                                    value={payPalEmail}
                                    onChangeText={setPayPalEmail}
                                    placeholder="your-paypal@email.com"
                                    placeholderTextColor="#999"
                                    keyboardType="email-address"
                                    autoCapitalize="none"
                                />
                            </View>
                            <View style={styles.inputContainer}>
                                <Text style={styles.label}>YouTube Account Age</Text>
                                <TextInput
                                    style={styles.input}
                                    value={youTubeAccountAge}
                                    onChangeText={setYouTubeAccountAge}
                                    placeholder="e.g. 5 years"
                                    placeholderTextColor="#999"
                                />
                            </View>
                            <View style={styles.inputContainer}>
                                <Text style={styles.label}>Comment Count Estimate</Text>
                                <TextInput
                                    style={styles.input}
                                    value={commentCountEstimate}
                                    onChangeText={setCommentCountEstimate}
                                    placeholder="e.g. ~5000 comments"
                                    placeholderTextColor="#999"
                                />
                            </View>
                            <View style={styles.inputContainer}>
                                <Text style={styles.label}>Content Types</Text>
                                <TextInput
                                    style={styles.input}
                                    value={contentTypes}
                                    onChangeText={setContentTypes}
                                    placeholder="e.g. YouTube comments, Spotify history"
                                    placeholderTextColor="#999"
                                />
                            </View>
                        </>
                    )}

                    {role === 'Buyer' && (
                        <>
                            <Text style={styles.sectionHeader}>Buyer Details</Text>
                            <View style={styles.inputContainer}>
                                <Text style={styles.label}>Company Name</Text>
                                <TextInput
                                    style={styles.input}
                                    value={companyName}
                                    onChangeText={setCompanyName}
                                    placeholder="Your company name"
                                    placeholderTextColor="#999"
                                />
                            </View>
                            <View style={styles.inputContainer}>
                                <Text style={styles.label}>Use Case</Text>
                                <TextInput
                                    style={[styles.input, styles.textArea]}
                                    value={useCase}
                                    onChangeText={setUseCase}
                                    placeholder="Describe your intended use of the data"
                                    placeholderTextColor="#999"
                                    multiline
                                    numberOfLines={3}
                                />
                            </View>
                        </>
                    )}

                    <TouchableOpacity
                        style={[styles.button, loading && styles.buttonDisabled]}
                        onPress={handleRegister}
                        disabled={loading}
                    >
                        {loading ? (
                            <ActivityIndicator color="#fff" />
                        ) : (
                            <Text style={styles.buttonText}>Create Account</Text>
                        )}
                    </TouchableOpacity>

                    <View style={styles.footer}>
                        <Text style={styles.footerText}>Already have an account? </Text>
                        <TouchableOpacity onPress={() => navigation.navigate('Login')}>
                            <Text style={styles.link}>Sign In</Text>
                        </TouchableOpacity>
                    </View>
                </View>
            </ScrollView>
        </KeyboardAvoidingView>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
    },
    scrollContent: {
        flexGrow: 1,
        justifyContent: 'center',
    },
    formContainer: {
        padding: 20,
        margin: 20,
        backgroundColor: '#fff',
        borderRadius: 10,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    title: {
        fontSize: 32,
        fontWeight: 'bold',
        color: '#333',
        textAlign: 'center',
        marginBottom: 5,
    },
    subtitle: {
        fontSize: 20,
        color: '#666',
        textAlign: 'center',
        marginBottom: 30,
    },
    sectionHeader: {
        fontSize: 16,
        fontWeight: '600',
        color: '#1a2f4a',
        marginTop: 10,
        marginBottom: 15,
    },
    inputContainer: {
        marginBottom: 15,
    },
    label: {
        fontSize: 14,
        fontWeight: '600',
        color: '#333',
        marginBottom: 8,
    },
    input: {
        borderWidth: 1,
        borderColor: '#ddd',
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
        color: '#333',
    },
    textArea: {
        height: 80,
        textAlignVertical: 'top',
    },
    roleContainer: {
        flexDirection: 'row',
        gap: 10,
    },
    roleButton: {
        flex: 1,
        padding: 12,
        borderRadius: 8,
        borderWidth: 2,
        borderColor: '#ddd',
        alignItems: 'center',
    },
    roleButtonActive: {
        borderColor: '#1a2f4a',
        backgroundColor: '#1a2f4a10',
    },
    roleButtonText: {
        fontSize: 14,
        fontWeight: '600',
        color: '#666',
    },
    roleButtonTextActive: {
        color: '#1a2f4a',
    },
    button: {
        backgroundColor: '#1a2f4a',
        padding: 15,
        borderRadius: 8,
        alignItems: 'center',
        marginTop: 10,
    },
    buttonDisabled: {
        backgroundColor: '#1a2f4a80',
    },
    buttonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    footer: {
        flexDirection: 'row',
        justifyContent: 'center',
        marginTop: 20,
    },
    footerText: {
        color: '#666',
        fontSize: 14,
    },
    link: {
        color: '#1a2f4a',
        fontSize: 14,
        fontWeight: '600',
    },
});

export default RegisterScreen;
