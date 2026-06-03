import React, { useState } from 'react';
import {
    View,
    Text,
    StyleSheet,
    TouchableOpacity,
    Alert,
    ActivityIndicator,
    Linking,
} from 'react-native';
import * as DocumentPicker from 'expo-document-picker';
import { uploadFile } from '../api/client';

const UploadScreen = ({ navigation }) => {
    const [selectedFile, setSelectedFile] = useState(null);
    const [uploading, setUploading] = useState(false);
    const [progress, setProgress] = useState(0);

    const openGoogleTakeout = async () => {
        try {
            const url = 'https://takeout.google.com';
            const canOpen = await Linking.canOpenURL(url);
            if (canOpen) {
                await Linking.openURL(url);
            } else {
                Alert.alert('Error', 'Cannot open URL');
            }
        } catch (error) {
            console.error('Error opening URL:', error);
            Alert.alert('Error', 'Failed to open Google Takeout');
        }
    };

    const pickFile = async () => {
        try {
            const result = await DocumentPicker.getDocumentAsync({
                type: 'application/zip',
                copyToCacheDirectory: true,
            });

            if (result.canceled) {
                return;
            }

            const file = result.assets[0];

            // Check if it's a ZIP file
            if (!file.name.toLowerCase().endsWith('.zip')) {
                Alert.alert('Error', 'Please select a ZIP file');
                return;
            }

            setSelectedFile(file);
        } catch (error) {
            console.error('Error picking file:', error);
            Alert.alert('Error', 'Failed to pick file');
        }
    };

    const handleUpload = async () => {
        if (!selectedFile) {
            Alert.alert('Error', 'Please select a file first');
            return;
        }

        setUploading(true);
        setProgress(0);

        try {
            // Create form data
            const formData = new FormData();
            formData.append('file', {
                uri: selectedFile.uri,
                type: 'application/zip',
                name: selectedFile.name,
            });

            await uploadFile(formData, (progressPercent) => {
                setProgress(progressPercent);
            });

            Alert.alert(
                'Success',
                'File uploaded successfully!',
                [
                    {
                        text: 'OK',
                        onPress: () => {
                            setSelectedFile(null);
                            setProgress(0);
                            navigation.navigate('Dashboard');
                        },
                    },
                ]
            );
        } catch (error) {
            console.error('Upload error:', error);
            const errorMessage = error.response?.data?.message || 'Failed to upload file';
            Alert.alert('Error', errorMessage);
        } finally {
            setUploading(false);
        }
    };

    return (
        <View style={styles.container}>
            <Text style={styles.title}>Upload Your Data</Text>

            <View style={styles.card}>
                <Text style={styles.sectionTitle}>Step 1: Download Your Data from Google</Text>
                <Text style={styles.description}>
                    Go to Google Takeout to download a copy of your Google data including YouTube watch history and comments.
                </Text>
                <TouchableOpacity style={styles.button} onPress={openGoogleTakeout}>
                    <Text style={styles.buttonText}>Open Google Takeout</Text>
                </TouchableOpacity>
            </View>

            <View style={styles.card}>
                <Text style={styles.sectionTitle}>Step 2: Select Your ZIP File</Text>
                <Text style={styles.description}>
                    Select the ZIP file containing your Google Takeout data.
                </Text>
                <TouchableOpacity
                    style={[styles.button, selectedFile && styles.buttonSecondary]}
                    onPress={pickFile}
                    disabled={uploading}
                >
                    <Text style={styles.buttonText}>
                        {selectedFile ? 'Change File' : 'Select ZIP File'}
                    </Text>
                </TouchableOpacity>

                {selectedFile && (
                    <View style={styles.fileInfo}>
                        <Text style={styles.fileName} numberOfLines={1}>
                            Selected: {selectedFile.name}
                        </Text>
                    </View>
                )}
            </View>

            {uploading && (
                <View style={styles.progressContainer}>
                    <Text style={styles.progressText}>Uploading... {progress}%</Text>
                    <View style={styles.progressBarContainer}>
                        <View style={[styles.progressBar, { width: `${progress}%` }]} />
                    </View>
                </View>
            )}

            <View style={styles.uploadContainer}>
                <TouchableOpacity
                    style={[
                        styles.uploadButton,
                        (!selectedFile || uploading) && styles.uploadButtonDisabled,
                    ]}
                    onPress={handleUpload}
                    disabled={!selectedFile || uploading}
                >
                    {uploading ? (
                        <ActivityIndicator color="#fff" />
                    ) : (
                        <Text style={styles.uploadButtonText}>Submit File</Text>
                    )}
                </TouchableOpacity>
            </View>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f5f5f5',
        padding: 20,
    },
    title: {
        fontSize: 24,
        fontWeight: 'bold',
        color: '#333',
        marginBottom: 20,
    },
    card: {
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
    sectionTitle: {
        fontSize: 16,
        fontWeight: '600',
        color: '#333',
        marginBottom: 10,
    },
    description: {
        fontSize: 14,
        color: '#666',
        lineHeight: 20,
        marginBottom: 15,
    },
    button: {
        backgroundColor: '#007AFF',
        padding: 12,
        borderRadius: 8,
        alignItems: 'center',
    },
    buttonSecondary: {
        backgroundColor: '#34C759',
    },
    buttonText: {
        color: '#fff',
        fontSize: 14,
        fontWeight: '600',
    },
    fileInfo: {
        marginTop: 10,
        padding: 10,
        backgroundColor: '#f0f0f0',
        borderRadius: 5,
    },
    fileName: {
        fontSize: 12,
        color: '#333',
    },
    progressContainer: {
        marginBottom: 20,
    },
    progressText: {
        fontSize: 14,
        color: '#333',
        marginBottom: 8,
    },
    progressBarContainer: {
        height: 8,
        backgroundColor: '#e0e0e0',
        borderRadius: 4,
        overflow: 'hidden',
    },
    progressBar: {
        height: '100%',
        backgroundColor: '#34C759',
        borderRadius: 4,
    },
    uploadContainer: {
        marginTop: 'auto',
    },
    uploadButton: {
        backgroundColor: '#007AFF',
        padding: 15,
        borderRadius: 8,
        alignItems: 'center',
    },
    uploadButtonDisabled: {
        backgroundColor: '#007AFF80',
    },
    uploadButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
});

export default UploadScreen;
