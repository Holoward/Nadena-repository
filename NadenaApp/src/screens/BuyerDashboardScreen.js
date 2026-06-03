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
    Linking,
    Modal,
    ScrollView,
} from 'react-native';
import { getAllDatasets, createCheckoutSession, getDatasetPreview } from '../api/client';

const BuyerDashboardScreen = ({ navigation }) => {
    const [datasets, setDatasets] = useState([]);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [purchasingId, setPurchasingId] = useState(null);
    const [previewVisible, setPreviewVisible] = useState(false);
    const [previewData, setPreviewData] = useState([]);
    const [previewLoading, setPreviewLoading] = useState(false);
    const [previewTitle, setPreviewTitle] = useState('');

    const fetchDatasets = async () => {
        try {
            const data = await getAllDatasets();
            setDatasets(data);
        } catch (error) {
            console.error('Error fetching datasets:', error);
            Alert.alert('Error', 'Failed to load datasets');
            if (error.response?.status === 401) {
                navigation.replace('Login');
            }
        } finally {
            setLoading(false);
            setRefreshing(false);
        }
    };

    useEffect(() => {
        fetchDatasets();
    }, []);

    const onRefresh = () => {
        setRefreshing(true);
        fetchDatasets();
    };

    const handlePreview = async (datasetId, datasetTitle) => {
        setPreviewTitle(datasetTitle);
        setPreviewVisible(true);
        setPreviewLoading(true);
        try {
            const data = await getDatasetPreview(datasetId);
            setPreviewData(data?.comments || data || []);
        } catch (error) {
            console.error('Preview error:', error);
            setPreviewData([]);
        } finally {
            setPreviewLoading(false);
        }
    };

    const handlePurchase = async (datasetId) => {
        setPurchasingId(datasetId);
        try {
            const result = await createCheckoutSession(datasetId);

            if (result?.url) {
                const canOpen = await Linking.canOpenURL(result.url);
                if (canOpen) {
                    await Linking.openURL(result.url);
                } else {
                    Alert.alert('Error', 'Cannot open payment URL');
                }
            } else {
                Alert.alert('Error', 'No payment URL returned');
            }
        } catch (error) {
            console.error('Purchase error:', error);
            const errorMessage = error.response?.data?.message || 'Failed to create checkout session';
            Alert.alert('Error', errorMessage);
        } finally {
            setPurchasingId(null);
        }
    };

    const renderDataset = ({ item }) => (
        <View style={styles.card}>
            <View style={styles.cardHeader}>
                <Text style={styles.cardTitle}>{item.title}</Text>
                <Text style={styles.price}>${item.price?.toFixed(2) || '0.00'}</Text>
            </View>

            <Text style={styles.description} numberOfLines={3}>
                {item.description}
            </Text>

            <View style={styles.cardFooter}>
                <View style={styles.stats}>
                    <Text style={styles.statText}>
                        {item.commentCount || 0} comments
                    </Text>
                </View>

                <View style={styles.actions}>
                    <TouchableOpacity
                        style={styles.previewButton}
                        onPress={() => handlePreview(item.id, item.title)}
                    >
                        <Text style={styles.previewButtonText}>Preview</Text>
                    </TouchableOpacity>
                    <TouchableOpacity
                        style={[
                            styles.purchaseButton,
                            purchasingId === item.id && styles.purchaseButtonDisabled,
                        ]}
                        onPress={() => handlePurchase(item.id)}
                        disabled={purchasingId === item.id}
                    >
                        {purchasingId === item.id ? (
                            <ActivityIndicator color="#fff" size="small" />
                        ) : (
                            <Text style={styles.purchaseButtonText}>Purchase</Text>
                        )}
                    </TouchableOpacity>
                </View>
            </View>
        </View>
    );

    if (loading) {
        return (
            <View style={styles.centerContainer}>
                <ActivityIndicator size="large" color="#1a2f4a" />
                <Text style={styles.loadingText}>Loading datasets...</Text>
            </View>
        );
    }

    return (
        <View style={styles.container}>
            <Text style={styles.headerTitle}>Datasets</Text>

            {datasets.length === 0 ? (
                <View style={styles.emptyContainer}>
                    <Text style={styles.emptyText}>No datasets available yet</Text>
                </View>
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

            {/* Preview Modal */}
            <Modal
                visible={previewVisible}
                animationType="slide"
                transparent={true}
                onRequestClose={() => setPreviewVisible(false)}
            >
                <View style={styles.modalOverlay}>
                    <View style={styles.modalContent}>
                        <View style={styles.modalHeader}>
                            <Text style={styles.modalTitle}>Preview: {previewTitle}</Text>
                            <TouchableOpacity onPress={() => setPreviewVisible(false)}>
                                <Text style={styles.modalClose}>Close</Text>
                            </TouchableOpacity>
                        </View>
                        {previewLoading ? (
                            <ActivityIndicator size="large" color="#1a2f4a" style={styles.modalLoading} />
                        ) : previewData.length === 0 ? (
                            <Text style={styles.noPreviewText}>No preview data available</Text>
                        ) : (
                            <ScrollView style={styles.previewList}>
                                {previewData.slice(0, 5).map((item, index) => (
                                    <View key={index} style={styles.previewItem}>
                                        <Text style={styles.previewText}>
                                            {item.commentText || item.comment || item.trackName || JSON.stringify(item)}
                                        </Text>
                                    </View>
                                ))}
                            </ScrollView>
                        )}
                    </View>
                </View>
            </Modal>
        </View>
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
    loadingText: {
        marginTop: 10,
        fontSize: 16,
        color: '#666',
    },
    headerTitle: {
        fontSize: 24,
        fontWeight: 'bold',
        color: '#1a2f4a',
        padding: 20,
        paddingBottom: 10,
    },
    listContent: {
        padding: 20,
        paddingTop: 0,
    },
    card: {
        backgroundColor: '#fff',
        borderRadius: 10,
        padding: 16,
        marginBottom: 15,
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.1,
        shadowRadius: 4,
        elevation: 3,
    },
    cardHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        marginBottom: 10,
    },
    cardTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: '#1a2f4a',
        flex: 1,
        marginRight: 10,
    },
    price: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#34C759',
    },
    description: {
        fontSize: 14,
        color: '#666',
        lineHeight: 20,
        marginBottom: 15,
    },
    cardFooter: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
    },
    stats: {
        flex: 1,
    },
    statText: {
        fontSize: 12,
        color: '#999',
    },
    actions: {
        flexDirection: 'row',
        gap: 8,
    },
    previewButton: {
        backgroundColor: '#f0f0f0',
        paddingVertical: 10,
        paddingHorizontal: 16,
        borderRadius: 8,
    },
    previewButtonText: {
        color: '#1a2f4a',
        fontSize: 14,
        fontWeight: '600',
    },
    purchaseButton: {
        backgroundColor: '#1a2f4a',
        paddingVertical: 10,
        paddingHorizontal: 20,
        borderRadius: 8,
        minWidth: 100,
        alignItems: 'center',
    },
    purchaseButtonDisabled: {
        backgroundColor: '#1a2f4a80',
    },
    purchaseButtonText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    emptyContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
    },
    emptyText: {
        fontSize: 16,
        color: '#999',
    },
    // Modal styles
    modalOverlay: {
        flex: 1,
        backgroundColor: 'rgba(0,0,0,0.5)',
        justifyContent: 'flex-end',
    },
    modalContent: {
        backgroundColor: '#fff',
        borderTopLeftRadius: 20,
        borderTopRightRadius: 20,
        maxHeight: '70%',
        padding: 20,
    },
    modalHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 15,
    },
    modalTitle: {
        fontSize: 18,
        fontWeight: '600',
        color: '#1a2f4a',
        flex: 1,
    },
    modalClose: {
        fontSize: 16,
        color: '#1a2f4a',
        fontWeight: '600',
    },
    modalLoading: {
        marginTop: 40,
    },
    noPreviewText: {
        textAlign: 'center',
        color: '#999',
        fontSize: 14,
        marginTop: 40,
    },
    previewList: {
        maxHeight: 400,
    },
    previewItem: {
        backgroundColor: '#f9f9f9',
        borderRadius: 8,
        padding: 12,
        marginBottom: 10,
    },
    previewText: {
        fontSize: 14,
        color: '#333',
        lineHeight: 20,
    },
});

export default BuyerDashboardScreen;
