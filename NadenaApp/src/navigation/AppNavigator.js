import React, { useState, useEffect, useCallback } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { View, ActivityIndicator, StyleSheet } from 'react-native';
import { jwtDecode } from 'jwt-decode';
import { Ionicons } from '@expo/vector-icons';

import { getToken } from '../api/client';
import { navigationRef } from './navigationRef';
import LoginScreen from '../screens/LoginScreen';
import RegisterScreen from '../screens/RegisterScreen';
import VolunteerDashboardScreen from '../screens/VolunteerDashboardScreen';
import UploadScreen from '../screens/UploadScreen';
import BuyerDashboardScreen from '../screens/BuyerDashboardScreen';
import AdminDashboardScreen from '../screens/AdminDashboardScreen';
import ProfileScreen from '../screens/ProfileScreen';

const Stack = createStackNavigator();
const Tab = createBottomTabNavigator();

// Auth Stack Navigator
const AuthStack = () => (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
        <Stack.Screen name="Login" component={LoginScreen} />
        <Stack.Screen name="Register" component={RegisterScreen} />
    </Stack.Navigator>
);

// Volunteer Tab Navigator
const VolunteerTabs = () => (
    <Tab.Navigator
        screenOptions={({ route }) => ({
            tabBarIcon: ({ focused, color, size }) => {
                let iconName;

                if (route.name === 'Dashboard') {
                    iconName = focused ? 'home' : 'home-outline';
                } else if (route.name === 'Upload') {
                    iconName = focused ? 'cloud-upload' : 'cloud-upload-outline';
                } else if (route.name === 'Profile') {
                    iconName = focused ? 'person' : 'person-outline';
                }

                return <Ionicons name={iconName} size={size} color={color} />;
            },
            tabBarActiveTintColor: '#1a2f4a',
            tabBarInactiveTintColor: '#999',
            headerShown: false,
        })}
    >
        <Tab.Screen
            name="Dashboard"
            component={VolunteerDashboardScreen}
            options={{ title: 'Dashboard' }}
        />
        <Tab.Screen
            name="Upload"
            component={UploadScreen}
            options={{ title: 'Upload' }}
        />
        <Tab.Screen
            name="Profile"
            component={ProfileScreen}
            options={{ title: 'Profile' }}
        />
    </Tab.Navigator>
);

// Buyer Tab Navigator
const BuyerTabs = () => (
    <Tab.Navigator
        screenOptions={({ route }) => ({
            tabBarIcon: ({ focused, color, size }) => {
                let iconName;

                if (route.name === 'Datasets') {
                    iconName = focused ? 'folder' : 'folder-outline';
                } else if (route.name === 'Profile') {
                    iconName = focused ? 'person' : 'person-outline';
                }

                return <Ionicons name={iconName} size={size} color={color} />;
            },
            tabBarActiveTintColor: '#1a2f4a',
            tabBarInactiveTintColor: '#999',
            headerShown: false,
        })}
    >
        <Tab.Screen
            name="Datasets"
            component={BuyerDashboardScreen}
            options={{ title: 'Datasets' }}
        />
        <Tab.Screen
            name="Profile"
            component={ProfileScreen}
            options={{ title: 'Profile' }}
        />
    </Tab.Navigator>
);

// Admin Tab Navigator
const AdminTabs = () => (
    <Tab.Navigator
        screenOptions={({ route }) => ({
            tabBarIcon: ({ focused, color, size }) => {
                let iconName;

                if (route.name === 'AdminHome') {
                    iconName = focused ? 'speedometer' : 'speedometer-outline';
                } else if (route.name === 'Volunteers') {
                    iconName = focused ? 'people' : 'people-outline';
                } else if (route.name === 'AdminDatasets') {
                    iconName = focused ? 'folder' : 'folder-outline';
                } else if (route.name === 'Profile') {
                    iconName = focused ? 'person' : 'person-outline';
                }

                return <Ionicons name={iconName} size={size} color={color} />;
            },
            tabBarActiveTintColor: '#1a2f4a',
            tabBarInactiveTintColor: '#999',
            headerShown: false,
        })}
    >
        <Tab.Screen
            name="AdminHome"
            options={{ title: 'Dashboard' }}
        >
            {(props) => <AdminDashboardScreen {...props} initialTab="overview" />}
        </Tab.Screen>
        <Tab.Screen
            name="Volunteers"
            options={{ title: 'Volunteers' }}
        >
            {(props) => <AdminDashboardScreen {...props} initialTab="volunteers" />}
        </Tab.Screen>
        <Tab.Screen
            name="AdminDatasets"
            options={{ title: 'Datasets' }}
        >
            {(props) => <AdminDashboardScreen {...props} initialTab="datasets" />}
        </Tab.Screen>
        <Tab.Screen
            name="Profile"
            component={ProfileScreen}
            options={{ title: 'Profile' }}
        />
    </Tab.Navigator>
);

// Main App Navigator with Auth Check
const AppNavigator = () => {
    const [isLoading, setIsLoading] = useState(true);
    const [userRole, setUserRole] = useState(null);
    const [authKey, setAuthKey] = useState(0);

    useEffect(() => {
        checkAuth();
    }, [authKey]);

    const checkAuth = async () => {
        try {
            const token = await getToken();
            if (token) {
                const decoded = jwtDecode(token);
                const role = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
                setUserRole(role);
            } else {
                setUserRole(null);
            }
        } catch (error) {
            console.error('Error checking auth:', error);
            setUserRole(null);
        } finally {
            setIsLoading(false);
        }
    };

    // Expose a method to re-check auth after login
    const onStateChange = useCallback(() => {
        // Re-check auth when navigation state changes (e.g. after login)
        checkAuth();
    }, []);

    if (isLoading) {
        return (
            <View style={styles.loadingContainer}>
                <ActivityIndicator size="large" color="#1a2f4a" />
            </View>
        );
    }

    return (
        <NavigationContainer ref={navigationRef} onStateChange={onStateChange}>
            {userRole === 'Volunteer' ? (
                <VolunteerTabs />
            ) : userRole === 'Buyer' ? (
                <BuyerTabs />
            ) : userRole === 'Admin' ? (
                <AdminTabs />
            ) : (
                <AuthStack />
            )}
        </NavigationContainer>
    );
};

const styles = StyleSheet.create({
    loadingContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#f5f5f5',
    },
});

export default AppNavigator;
