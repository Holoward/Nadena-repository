import React, { useState, useEffect } from 'react';
import {
    View,
    Text,
    StyleSheet,
    TouchableOpacity,
    Alert,
    ActivityIndicator,
    ScrollView,
} from 'react-native';
import { jwtDecode } from 'jwt-decode';
import { logout, getToken, getVolunteerByUserId } from '../api/client';

const ProfileScreen = ({ navigation }) => {
    const [loading, setLoading] = useState(true);
    const [userData, setUserData] = useState(null);
    const [volunteerStatus, setVolunteerStatus] = useState(null);

    useEffect(() => {
        loadUserData();
    }, []);

    const loadUserData = async () => {
        try {
            const token = await getToken();
            if (!token) {
                navigation.replace('Login');
                return;
            }

            const decoded = jwtDecode(token);
            const userId = decoded.sub || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
            const email = decoded.email || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
            const role = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            setUserData({
                userId,
                email,
                role,
            });

            // If volunteer, get status
            if (role === 'Volunteer') {
                try {
                    const volunteerData = await getVolunteerByUserId(userId);
                    setVolunteerStatus(volunteerData?.status);
                } catch (err) {
                    console.error('Error fetching volunteer status:', err);
                }
            }
        } catch (error) {
            console.error('Error loading user data:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleLogout = () => {
        Alert.alert(
            'Logout',
            'Are you sure you want to logout?',
            [
                { text: 'Cancel', style: 'cancel' },
                {
                    text: 'Logout',
                    style: 'destructive',
                    onPress: async () => {
                        try {
                            await logout();
                            navigation.replace('Login');
                        } catch (error) {
                            console.error('Logout error:', error);
                            // Still navigate to login even if there's an error
                            navigation.replace('Login');
                        }
                    },
                },
            ]
        );
    };

    if (loading) {
        return (
            <View style={styles.centerContainer}>
                <ActivityIndicator size="large" color="#007AFF" />
                <Text style={styles.loadingText}>Loading profile...</Text>
            </View>
        );
    }

    return (
        <ScrollView style={styles.container}>
            <View style={styles.content}>
                <Text style={styles.title}>Profile</Text>

                <View style={styles.card}>
                    <View style={styles.avatar}>
                        <Text style={styles.avatarText}>
                            {userData?.email?.charAt(0).toUpperCase() || 'U'}
                        </Text>
                    </View>

                    <View style={styles.infoSection}>
                        <View style={styles.infoRow}>
                            <Text style={styles.label}>Email</Text>
                            <Text style={styles.value}>{userData?.email || 'N/A'}</Text>
                        </View>

                        <View style={styles.infoRow}>
                            <Text style={styles.label}>Role</Text>
                            <View style={styles.roleBadge}>
                                <Text style={styles.roleText}>{userData?.role || 'N/A'}</Text>
                            </View>
                        </View>

                        {userData?.role === 'Volunteer' && (
                            <View style={styles.infoRow}>
                                <Text style={styles.label}>Status</Text>
                                <Text style={styles.value}>{volunteerStatus || 'N/A'}</Text>
                            </View>
                        )}
                    </View>
                </View>

                <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
                    <Text style={styles.logoutButtonText}>Logout</Text>
                </TouchableOpacity>
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
        color: '#333',
        marginBottom: 20,
    },
    loadingText: {
        marginTop: 10,
        fontSize: 16,
        color: '#666',
    },
    card: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 20,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    avatar: {
        width: 80,
        height: 80,
        borderRadius: 40,
        backgroundColor: '#007AFF',
        justifyContent: 'center',
        alignItems: 'center',
        alignSelf: 'center',
        marginBottom: 20,
    },
    avatarText: {
        fontSize: 32,
        fontWeight: 'bold',
        color: '#fff',
    },
    infoSection: {
        marginTop: 10,
    },
    infoRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        paddingVertical: 12,
        borderBottomWidth: 1,
        borderBottomColor: '#f0f0f0',
    },
    label: {
        fontSize: 14,
        color: '#666',
    },
    value: {
        fontSize: 14,
        fontWeight: '600',
        color: '#333',
    },
    roleBadge: {
        backgroundColor: '#007AFF20',
        paddingVertical: 4,
        paddingHorizontal: 12,
        borderRadius: 12,
    },
    roleText: {
        fontSize: 12,
        fontWeight: '600',
        color: '#007AFF',
    },
    logoutButton: {
        backgroundColor: '#ff3b30',
        padding: 15,
        borderRadius: 8,
        alignItems: 'center',
        marginTop: 30,
    },
    logoutButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
});

export default ProfileScreen;
