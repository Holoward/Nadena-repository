import React, { useState, useEffect } from 'react';
import {
    View,
    Text,
    StyleSheet,
    FlatList,
    TouchableOpacity,
    ActivityIndicator,
    Alert,
    RefreshControl,
    ScrollView,
} from 'react-native';
import { getAllVolunteers, updateVolunteerStatus, getAllDatasets, createDataset } from '../../api/client';

const STATUS_OPTIONS = ['Registered', 'Activated', 'FileReceived', 'Paid'];

const OverviewTab = ({ stats, loading }) => {
    if (loading) {
        return (
            <View style={styles.tabContent}>
                <ActivityIndicator size="large" color="#1a2f4a" />
            </View>
        );
    }

    return (
        <ScrollView style={styles.tabContent}>
            <Text style={styles.tabTitle}>Overview</Text>
            <View style={styles.statsGrid}>
                <View style={[styles.statCard, { backgroundColor: '#1a2f4a' }]}>
                    <Text style={styles.statNumber}>{stats.totalVolunteers || 0}</Text>
                    <Text style={styles.statLabel}>Total Volunteers</Text>
                </View>
                <View style={[styles.statCard, { backgroundColor: '#34C759' }]}>
                    <Text style={styles.statNumber}>{stats.activated || 0}</Text>
                    <Text style={styles.statLabel}>Activated</Text>
                </View>
                <View style={[styles.statCard, { backgroundColor: '#FF9500' }]}>
                    <Text style={styles.statNumber}>{stats.filesReceived || 0}</Text>
                    <Text style={styles.statLabel}>Files Received</Text>
                </View>
                <View style={[styles.statCard, { backgroundColor: '#5856D6' }]}>
                    <Text style={styles.statNumber}>{stats.paid || 0}</Text>
                    <Text style={styles.statLabel}>Paid</Text>
                </View>
            </View>
        </ScrollView>
    );
};

const VolunteersTab = ({ volunteers, loading, onRefresh, refreshing }) => {
    const handleStatusChange = async (volunteerId, currentStatus) => {
        const currentIndex = STATUS_OPTIONS.findIndex(
            s => s.toLowerCase() === currentStatus?.toLowerCase()
        );
        const nextIndex = (currentIndex + 1) % STATUS_OPTIONS.length;
        const newStatus = STATUS_OPTIONS[nextIndex];

        Alert.alert(
            'Update Status',
            `Change status to "${newStatus}"?`,
            [
                { text: 'Cancel', style: 'cancel' },
                {
                    text: 'Update',
                    onPress: async () => {
                        try {
                            await updateVolunteerStatus(volunteerId, newStatus);
                            onRefresh();
                        } catch (error) {
                            console.error('Error updating status:', error);
                            Alert.alert('Error', 'Failed to update status');
                        }
                    },
                },
            ]
        );
    };

    const renderVolunteer = ({ item }) => (
        <View style={styles.volunteerCard}>
            <View style={styles.volunteerHeader}>
                <Text style={styles.volunteerName}>
                    {item.userId || 'Unknown'}
                </Text>
                <TouchableOpacity
                    style={styles.statusBadge}
                    onPress={() => handleStatusChange(item.id, item.status)}
                >
                    <Text style={styles.statusText}>{item.status || 'Unknown'}</Text>
                </TouchableOpacity>
            </View>
            <View style={styles.volunteerInfo}>
                {item.dataSourceType && (
                    <Text style={styles.volunteerDetail}>Source: {item.dataSourceType}</Text>
                )}
                {item.payPalEmail && (
                    <Text style={styles.volunteerDetail}>PayPal: {item.payPalEmail}</Text>
                )}
                {item.fileLink && (
                    <Text style={styles.volunteerDetail}>File: {item.fileLink}</Text>
                )}
            </View>
        </View>
    );

    if (loading) {
        return (
            <View style={styles.tabContent}>
                <ActivityIndicator size="large" color="#1a2f4a" />
            </View>
        );
    }

    return (
        <View style={styles.tabContent}>
            <Text style={styles.tabTitle}>Volunteers</Text>
            {volunteers.length === 0 ? (
                <Text style={styles.emptyText}>No volunteers found</Text>
            ) : (
                <FlatList
                    data={volunteers}
                    renderItem={renderVolunteer}
                    keyExtractor={(item) => item.id?.toString()}
                    contentContainerStyle={styles.listContent}
                    refreshControl={
                        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
                    }
                />
            )}
        </View>
    );
};

const DatasetsTab = ({ datasets, loading, onRefresh, refreshing }) => {
    const renderDataset = ({ item }) => (
        <View style={styles.datasetCard}>
            <View style={styles.datasetHeader}>
                <Text style={styles.datasetTitle}>{item.title}</Text>
                <Text style={styles.datasetPrice}>${item.price?.toFixed(2) || '0.00'}</Text>
            </View>
            <Text style={styles.datasetDescription} numberOfLines={2}>
                {item.description || 'No description'}
            </Text>
            <View style={styles.datasetStats}>
                <Text style={styles.datasetStat}>
                    {item.commentCount || 0} records
                </Text>
                <Text style={styles.datasetStat}>
                    {item.volunteerCount || 0} volunteers
                </Text>
                <Text style={styles.datasetStat}>
                    Status: {item.status || 'Active'}
                </Text>
            </View>
        </View>
    );

    if (loading) {
        return (
            <View style={styles.tabContent}>
                <ActivityIndicator size="large" color="#1a2f4a" />
            </View>
        );
    }

    return (
        <View style={styles.tabContent}>
            <Text style={styles.tabTitle}>Datasets</Text>
            {datasets.length === 0 ? (
                <Text style={styles.emptyText}>No datasets found</Text>
            ) : (
                <FlatList
                    data={datasets}
                    renderItem={renderDataset}
                    keyExtractor={(item) => item.id?.toString()}
                    contentContainerStyle={styles.listContent}
                    refreshControl={
                        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
                    }
                />
            )}
        </View>
    );
};

const AdminDashboardScreen = ({ navigation, initialTab = 'overview' }) => {
    const [activeTab, setActiveTab] = useState(initialTab);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [volunteers, setVolunteers] = useState([]);
    const [datasets, setDatasets] = useState([]);
    const [stats, setStats] = useState({});

    const fetchData = async () => {
        try {
            const [volunteersData, datasetsData] = await Promise.all([
                getAllVolunteers().catch(() => []),
                getAllDatasets().catch(() => []),
            ]);

            setVolunteers(Array.isArray(volunteersData) ? volunteersData : volunteersData?.data || []);
            setDatasets(Array.isArray(datasetsData) ? datasetsData : datasetsData?.data || []);

            // Calculate stats from volunteers
            const volunteerList = Array.isArray(volunteersData) ? volunteersData : [];
            setStats({
                totalVolunteers: volunteerList.length,
                activated: volunteerList.filter(v => v.status?.toLowerCase() === 'activated').length,
                filesReceived: volunteerList.filter(v => v.status?.toLowerCase() === 'filereceived').length,
                paid: volunteerList.filter(v => v.status?.toLowerCase() === 'paid').length,
            });
        } catch (error) {
            console.error('Error fetching admin data:', error);
            if (error.response?.status === 401) {
                navigation.replace('Login');
            }
        } finally {
            setLoading(false);
            setRefreshing(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, []);

    const onRefresh = () => {
        setRefreshing(true);
        fetchData();
    };

    return (
        <View style={styles.container}>
            <Text style={styles.headerTitle}>Admin Dashboard</Text>

            {/* Tab Bar */}
            <View style={styles.tabBar}>
                {[
                    { key: 'overview', label: 'Overview' },
                    { key: 'volunteers', label: 'Volunteers' },
                    { key: 'datasets', label: 'Datasets' },
                ].map((tab) => (
                    <TouchableOpacity
                        key={tab.key}
                        style={[
                            styles.tabButton,
                            activeTab === tab.key && styles.tabButtonActive,
                        ]}
                        onPress={() => setActiveTab(tab.key)}
                    >
                        <Text
                            style={[
                                styles.tabButtonText,
                                activeTab === tab.key && styles.tabButtonTextActive,
                            ]}
                        >
                            {tab.label}
                        </Text>
                    </TouchableOpacity>
                ))}
            </View>

            {/* Tab Content */}
            {activeTab === 'overview' && <OverviewTab stats={stats} loading={loading} />}
            {activeTab === 'volunteers' && (
                <VolunteersTab
                    volunteers={volunteers}
                    loading={loading}
                    onRefresh={onRefresh}
                    refreshing={refreshing}
                />
            )}
            {activeTab === 'datasets' && (
                <DatasetsTab
                    datasets={datasets}
                    loading={loading}
                    onRefresh={onRefresh}
                    refreshing={refreshing}
                />
            )}
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
    },
    headerTitle: {
        fontSize: 24,
        fontWeight: 'bold',
        color: '#1a2f4a',
        padding: 20,
        paddingBottom: 10,
    },
    tabBar: {
        flexDirection: 'row',
        backgroundColor: '#fff',
        marginHorizontal: 20,
        borderRadius: 10,
        padding: 4,
        marginBottom: 15,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    tabButton: {
        flex: 1,
        paddingVertical: 10,
        alignItems: 'center',
        borderRadius: 8,
    },
    tabButtonActive: {
        backgroundColor: '#1a2f4a',
    },
    tabButtonText: {
        fontSize: 14,
        fontWeight: '600',
        color: '#666',
    },
    tabButtonTextActive: {
        color: '#fff',
    },
    tabContent: {
        flex: 1,
        paddingHorizontal: 20,
    },
    tabTitle: {
        fontSize: 18,
        fontWeight: '600',
        color: '#1a2f4a',
        marginBottom: 15,
    },
    emptyText: {
        textAlign: 'center',
        color: '#999',
        fontSize: 14,
        marginTop: 40,
    },
    listContent: {
        paddingBottom: 20,
    },
    // Overview styles
    statsGrid: {
        flexDirection: 'row',
        flexWrap: 'wrap',
        gap: 12,
    },
    statCard: {
        width: '47%',
        borderRadius: 10,
        padding: 20,
        alignItems: 'center',
    },
    statNumber: {
        fontSize: 32,
        fontWeight: 'bold',
        color: '#fff',
    },
    statLabel: {
        fontSize: 12,
        color: '#ffffffcc',
        marginTop: 4,
    },
    // Volunteer styles
    volunteerCard: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 16,
        marginBottom: 10,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    volunteerHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 8,
    },
    volunteerName: {
        fontSize: 16,
        fontWeight: '600',
        color: '#1a2f4a',
    },
    statusBadge: {
        backgroundColor: '#1a2f4a',
        paddingVertical: 4,
        paddingHorizontal: 12,
        borderRadius: 12,
    },
    statusText: {
        color: '#fff',
        fontSize: 12,
        fontWeight: '600',
    },
    volunteerInfo: {
        marginTop: 4,
    },
    volunteerDetail: {
        fontSize: 12,
        color: '#666',
        marginBottom: 2,
    },
    // Dataset styles
    datasetCard: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 16,
        marginBottom: 10,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    datasetHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 8,
    },
    datasetTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: '#1a2f4a',
        flex: 1,
    },
    datasetPrice: {
        fontSize: 16,
        fontWeight: 'bold',
        color: '#34C759',
    },
    datasetDescription: {
        fontSize: 13,
        color: '#666',
        marginBottom: 10,
    },
    datasetStats: {
        flexDirection: 'row',
        gap: 16,
    },
    datasetStat: {
        fontSize: 11,
        color: '#999',
    },
});

export default AdminDashboardScreen;
