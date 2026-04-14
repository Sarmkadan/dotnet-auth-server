#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# dotnet-auth-server cURL Examples
# This script demonstrates common OAuth2 operations using cURL

set -e

# Configuration
AUTH_SERVER="https://localhost:7001"
CLIENT_ID="my-spa"
CLIENT_SECRET="secret-key"
REDIRECT_URI="https://localhost:3000/callback"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Print colored output
print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}\n"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# ===== Discovery Endpoints =====
print_header "OpenID Connect Discovery"

print_info "Fetching OIDC configuration..."
curl -s "$AUTH_SERVER/.well-known/openid-configuration" | jq '.' | head -20
print_success "Discovery metadata retrieved"

# ===== JWKS Endpoint =====
print_header "JSON Web Key Set (JWKS)"

print_info "Fetching public signing keys..."
curl -s "$AUTH_SERVER/.well-known/jwks.json" | jq '.'
print_success "JWKS retrieved"

# ===== PKCE Parameter Generation =====
print_header "Generate PKCE Parameters"

# Generate code_verifier (43-128 chars, URL-safe)
CODE_VERIFIER=$(openssl rand -base64 48 | tr -d '\n=+/' | cut -c1-128)
print_success "Code Verifier: $CODE_VERIFIER"

# Generate code_challenge (S256 = base64url(sha256(verifier)))
CODE_CHALLENGE=$(echo -n "$CODE_VERIFIER" | \
    openssl dgst -sha256 -binary | \
    openssl enc -base64 | \
    tr -d '=\n' | \
    tr '+/' '-_')
print_success "Code Challenge: $CODE_CHALLENGE"

# ===== Authorization Endpoint =====
print_header "Authorization Request"

STATE="random_state_$(date +%s)"
NONCE="nonce_$(date +%s)"

AUTH_URL="$AUTH_SERVER/oauth/authorize?"\
"client_id=$CLIENT_ID&"\
"response_type=code&"\
"redirect_uri=$(urlencode $REDIRECT_URI)&"\
"scope=openid%20profile%20email&"\
"code_challenge=$CODE_CHALLENGE&"\
"code_challenge_method=S256&"\
"state=$STATE&"\
"nonce=$NONCE"

print_info "Open this URL in your browser to authorize:"
echo -e "${BLUE}$AUTH_URL${NC}"
print_info "After authorization, you'll be redirected with authorization code"

# Read authorization code from user
read -p "Enter authorization code: " AUTH_CODE

if [ -z "$AUTH_CODE" ]; then
    print_error "Authorization code required"
    exit 1
fi

# ===== Token Exchange =====
print_header "Exchange Authorization Code for Tokens"

TOKEN_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/oauth/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=authorization_code" \
    -d "client_id=$CLIENT_ID" \
    -d "redirect_uri=$REDIRECT_URI" \
    -d "code=$AUTH_CODE" \
    -d "code_verifier=$CODE_VERIFIER")

echo "$TOKEN_RESPONSE" | jq '.'

ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.access_token')
REFRESH_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.refresh_token')
ID_TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.id_token // empty')

if [ -z "$ACCESS_TOKEN" ] || [ "$ACCESS_TOKEN" = "null" ]; then
    print_error "Failed to obtain access token"
    exit 1
fi

print_success "Access token obtained: ${ACCESS_TOKEN:0:50}..."
print_success "Refresh token obtained: ${REFRESH_TOKEN:0:50}..."

# ===== Decode JWT (Client-side only) =====
print_header "Decode Access Token"

# Simple JWT decode (without signature verification)
decode_jwt() {
    local token="$1"
    local payload=$(echo "$token" | cut -d'.' -f2)

    # Add padding if needed
    local padding=$((4 - ${#payload} % 4))
    if [ $padding -lt 4 ]; then
        payload="${payload}$(printf '%.0s=' $(seq 1 $padding))"
    fi

    echo "$payload" | openssl enc -base64 -d | jq '.'
}

print_info "Decoded Access Token Claims:"
decode_jwt "$ACCESS_TOKEN"

# ===== Token Introspection =====
print_header "Token Introspection"

print_info "Validating token with auth server..."
INTROSPECT_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/oauth/token/introspect" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "token=$ACCESS_TOKEN")

echo "$INTROSPECT_RESPONSE" | jq '.'

ACTIVE=$(echo "$INTROSPECT_RESPONSE" | jq -r '.active')
if [ "$ACTIVE" = "true" ]; then
    print_success "Token is valid and active"
else
    print_error "Token is invalid or expired"
fi

# ===== Token Refresh =====
print_header "Token Refresh (Rotation)"

print_info "Refreshing access token..."
REFRESH_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/oauth/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=refresh_token" \
    -d "client_id=$CLIENT_ID" \
    -d "refresh_token=$REFRESH_TOKEN")

echo "$REFRESH_RESPONSE" | jq '.'

NEW_ACCESS_TOKEN=$(echo "$REFRESH_RESPONSE" | jq -r '.access_token')
NEW_REFRESH_TOKEN=$(echo "$REFRESH_RESPONSE" | jq -r '.refresh_token')

print_success "New access token: ${NEW_ACCESS_TOKEN:0:50}..."
print_success "New refresh token: ${NEW_REFRESH_TOKEN:0:50}..."
print_info "Old refresh token is now INVALID (rotation)"

# Update tokens for next examples
ACCESS_TOKEN="$NEW_ACCESS_TOKEN"
REFRESH_TOKEN="$NEW_REFRESH_TOKEN"

# ===== Using Access Token =====
print_header "Using Access Token with API"

print_info "Example: Calling protected API with Bearer token"
echo "curl -H \"Authorization: Bearer \$ACCESS_TOKEN\" https://api.example.com/user/profile"
echo ""
echo "In real usage:"
DUMMY_API="https://api.example.com/user/profile"
# This will fail since API doesn't exist, but shows the pattern
curl -s -H "Authorization: Bearer $ACCESS_TOKEN" "$DUMMY_API" 2>/dev/null || \
    print_info "(API endpoint doesn't exist in this example)"

# ===== Token Revocation =====
print_header "Token Revocation"

print_info "Revoking access token..."
REVOKE_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/oauth/token/revoke" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "token=$ACCESS_TOKEN" \
    -d "token_type_hint=access_token")

if [ -z "$REVOKE_RESPONSE" ]; then
    print_success "Token revoked successfully (empty response expected)"
else
    echo "$REVOKE_RESPONSE" | jq '.'
fi

# Verify token is revoked
print_info "Verifying token is revoked..."
VERIFY_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/oauth/token/introspect" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "token=$ACCESS_TOKEN")

STILL_ACTIVE=$(echo "$VERIFY_RESPONSE" | jq -r '.active')
if [ "$STILL_ACTIVE" = "false" ]; then
    print_success "Token confirmed as revoked"
else
    print_error "Token is still active (unexpected)"
fi

# ===== Client Credentials Flow =====
print_header "Client Credentials Flow (M2M)"

print_info "Requesting token without user interaction..."
CC_RESPONSE=$(curl -s -X POST "$AUTH_SERVER/oauth/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=client_credentials" \
    -d "client_id=$CLIENT_ID" \
    -d "client_secret=$CLIENT_SECRET" \
    -d "scope=api:read api:write")

echo "$CC_RESPONSE" | jq '.'

CC_TOKEN=$(echo "$CC_RESPONSE" | jq -r '.access_token')
if [ ! -z "$CC_TOKEN" ] && [ "$CC_TOKEN" != "null" ]; then
    print_success "Client credentials token obtained: ${CC_TOKEN:0:50}..."
fi

# ===== Summary =====
print_header "Summary"

echo "Endpoints:"
echo "  Authorization: $AUTH_SERVER/oauth/authorize"
echo "  Token: $AUTH_SERVER/oauth/token"
echo "  Introspection: $AUTH_SERVER/oauth/token/introspect"
echo "  Revocation: $AUTH_SERVER/oauth/token/revoke"
echo "  Discovery: $AUTH_SERVER/.well-known/openid-configuration"
echo "  JWKS: $AUTH_SERVER/.well-known/jwks.json"
echo ""
echo "Best Practices:"
echo "  • Always use HTTPS in production"
echo "  • Use PKCE for all client types"
echo "  • Store tokens securely (HttpOnly cookies for web)"
echo "  • Refresh tokens before expiration (add 5-min buffer)"
echo "  • Validate token signature in resource server"
echo "  • Use short-lived access tokens (1-24 hours)"
echo "  • Implement token refresh in background"
echo ""
print_success "Examples completed!"
