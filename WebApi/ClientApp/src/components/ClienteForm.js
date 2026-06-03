import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { useNavigate, useParams } from 'react-router-dom';

export function ClienteForm() {
    const { id } = useParams();
    const navigate = useNavigate();
    const isEditing = !!id;

    const [formData, setFormData] = useState({
        nombre: '',
        apellido: '',
        fechaNacimiento: '',
        telefono: '',
        email: '',
        direction: ''
    });

    useEffect(() => {
        if (isEditing) {
            fetchCliente(id);
        }
    }, [id]);

    const fetchCliente = async (clienteId) => {
        try {
            const response = await axios.get(`/api/v1/Clientes/${clienteId}`);
            if (response.data && response.data.data) {
                const c = response.data.data;
                setFormData({
                    nombre: c.nombre || '',
                    apellido: c.apellido || '',
                    fechaNacimiento: c.fechaNacimiento ? c.fechaNacimiento.split('T')[0] : '', // Format for date input
                    telefono: c.telefono || '',
                    email: c.email || '',
                    direction: c.direction || ''
                });
            }
        } catch (error) {
            console.error("Error fetching cliente", error);
        }
    };

    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData(prevState => ({
            ...prevState,
            [name]: value
        }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            if (isEditing) {
                await axios.put(`/api/v1/Clientes/${id}`, { id: parseInt(id), ...formData });
            } else {
                await axios.post('/api/v1/Clientes', formData);
            }
            navigate('/clientes');
        } catch (error) {
            console.error("Error saving cliente", error);
            alert("An error occurred while saving.");
        }
    };

    return (
        <div>
            <h1>{isEditing ? 'Edit Client' : 'Add New Client'}</h1>
            <form onSubmit={handleSubmit}>
                <div className="mb-3">
                    <label className="form-label">Nombre</label>
                    <input type="text" className="form-control" name="nombre" value={formData.nombre} onChange={handleChange} required />
                </div>
                <div className="mb-3">
                    <label className="form-label">Apellido</label>
                    <input type="text" className="form-control" name="apellido" value={formData.apellido} onChange={handleChange} required />
                </div>
                <div className="mb-3">
                    <label className="form-label">Fecha de Nacimiento</label>
                    <input type="date" className="form-control" name="fechaNacimiento" value={formData.fechaNacimiento} onChange={handleChange} required />
                </div>
                <div className="mb-3">
                    <label className="form-label">Telefono</label>
                    <input type="text" className="form-control" name="telefono" value={formData.telefono} onChange={handleChange} required />
                </div>
                <div className="mb-3">
                    <label className="form-label">Email</label>
                    <input type="email" className="form-control" name="email" value={formData.email} onChange={handleChange} required />
                </div>
                <div className="mb-3">
                    <label className="form-label">Dirección</label>
                    <input type="text" className="form-control" name="direction" value={formData.direction} onChange={handleChange} required />
                </div>
                
                <button type="submit" className="btn btn-primary">{isEditing ? 'Update' : 'Create'}</button>
                <button type="button" className="btn btn-secondary ms-2" onClick={() => navigate('/clientes')}>Cancel</button>
            </form>
        </div>
    );
}
