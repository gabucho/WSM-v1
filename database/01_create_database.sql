-- Ejecutar conectado a PostgreSQL como usuario administrador.
CREATE USER wms_user WITH PASSWORD 'CAMBIAR_PASSWORD';
CREATE DATABASE wms_db OWNER wms_user;
GRANT ALL PRIVILEGES ON DATABASE wms_db TO wms_user;
