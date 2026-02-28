#!/bin/bash

# Script to generate self-signed certificates for local development
# Run this before starting the docker-compose stack

echo "Creating self-signed certificates for local development..."

# Create certs directory if it doesn't exist
mkdir -p certs

# Read hostname from .env file or use default
HOSTNAME=${HOSTNAME:-localhost}
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

echo "Generating certificates for hostname: ${HOSTNAME}"

# Generate private key
openssl genrsa -out certs/server.key 2048

# Generate certificate signing request
openssl req -new -key certs/server.key -out certs/server.csr -subj "/C=US/ST=Local/L=Lab/O=Development/CN=${HOSTNAME}"

# Generate self-signed certificate valid for 365 days
openssl x509 -req -in certs/server.csr -signkey certs/server.key -out certs/server.crt -days 365 -extensions v3_req -extfile <(
cat <<EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
C = US
ST = Local
L = Lab
O = Development
CN = ${HOSTNAME}

[v3_req]
keyUsage = nonRepudiation, digitalSignature, keyEncipherment
subjectAltName = @alt_names

[alt_names]
DNS.1 = ${HOSTNAME}
DNS.2 = *.${HOSTNAME}
DNS.3 = localhost
DNS.4 = n8n.${HOSTNAME}
DNS.5 = traefik.${HOSTNAME}
IP.1 = 127.0.0.1
EOF
)

# Clean up CSR file
rm certs/server.csr

echo "Certificates generated successfully!"
echo "Files created:"
echo "  - certs/server.key (private key)"
echo "  - certs/server.crt (certificate)"
echo ""
echo "Next steps:"
echo "1. Update HOSTNAME in .env file if needed (currently: ${HOSTNAME})"
echo "2. Add '${HOSTNAME}' to your /etc/hosts file pointing to 127.0.0.1"
echo "3. Run: docker-compose up -d"
echo "4. Access N8N at: https://n8n.${HOSTNAME}"
echo "5. Access Traefik dashboard at: https://traefik.${HOSTNAME}"
echo ""
echo "Note: Your browser will show security warnings for self-signed certificates."
echo "You can safely proceed/accept the certificate for local development."