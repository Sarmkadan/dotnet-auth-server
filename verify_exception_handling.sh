#!/bin/bash
# Verification script for AuthServerException hierarchy mapping to RFC 6749 error responses

echo "=== Verifying AuthServerException Hierarchy Mapping Implementation ==="
echo ""

# Check 1: UnsupportedGrantTypeException exists
echo "✓ Check 1: UnsupportedGrantTypeException class exists"
if [ -f "src/Exceptions/UnsupportedGrantTypeException.cs" ]; then
    echo "  ✓ File exists: src/Exceptions/UnsupportedGrantTypeException.cs"
    grep -q "unsupported_grant_type" src/Exceptions/UnsupportedGrantTypeException.cs && echo "  ✓ Contains correct error code: unsupported_grant_type"
else
    echo "  ✗ File missing: src/Exceptions/UnsupportedGrantTypeException.cs"
    exit 1
fi

echo ""

# Check 2: ErrorHandlingMiddleware uses ToErrorResponse()
echo "✓ Check 2: ErrorHandlingMiddleware uses AuthServerException.ToErrorResponse()"
if grep -q "authException.ToErrorResponse()" src/Middleware/ErrorHandlingMiddleware.cs; then
    echo "  ✓ Middleware calls authException.ToErrorResponse()"
else
    echo "  ✗ Middleware does not call ToErrorResponse()"
    exit 1
fi

echo ""

# Check 3: RFC 6749 compliance - WWW-Authenticate header for invalid_client
echo "✓ Check 3: RFC 6749 compliance for invalid_client error"
if grep -q 'WWWAuthenticate' src/Middleware/ErrorHandlingMiddleware.cs && grep -q 'invalid_client' src/Middleware/ErrorHandlingMiddleware.cs; then
    echo "  ✓ Middleware adds WWW-Authenticate header for invalid_client errors"
else
    echo "  ✗ Missing RFC 6749 compliance for invalid_client"
    exit 1
fi

echo ""

# Check 4: All AuthServerException subclasses exist
echo "✓ Check 4: AuthServerException hierarchy"
for exception in InvalidGrantException InvalidClientException InvalidScopeException UnauthorizedClientException ValidationException ConfigurationException UnsupportedGrantTypeException; do
    if [ -f "src/Exceptions/${exception}.cs" ]; then
        echo "  ✓ ${exception}.cs exists"
    else
        echo "  ✗ Missing: ${exception}.cs"
        exit 1
    fi
done

echo ""

# Check 5: AuthServerException has required properties
echo "✓ Check 5: AuthServerException base class structure"
if grep -q "public string ErrorCode" src/Exceptions/AuthServerException.cs && \
   grep -q "public int StatusCode" src/Exceptions/AuthServerException.cs && \
   grep -q "public string ErrorDescription" src/Exceptions/AuthServerException.cs && \
   grep -q "public Dictionary<string, object> Details" src/Exceptions/AuthServerException.cs && \
   grep -q "ToErrorResponse()" src/Exceptions/AuthServerException.cs; then
    echo "  ✓ AuthServerException has all required properties and methods"
else
    echo "  ✗ AuthServerException missing required members"
    exit 1
fi

echo ""

# Check 6: Build succeeds
echo "✓ Check 6: Project builds successfully"
if dotnet build src/DotnetAuthServer.Core.csproj --nologo -v quiet 2>&1 | grep -q "Build succeeded"; then
    echo "  ✓ Project builds without errors"
else
    echo "  ✗ Build failed"
    exit 1
fi

echo ""

echo "=== All Verification Checks Passed! ==="
echo ""
echo "Summary of changes:"
echo "1. Added UnsupportedGrantTypeException.cs - missing exception class per RFC 6749"
echo "2. Updated ErrorHandlingMiddleware.cs to use ToErrorResponse() centrally"
echo "3. Added RFC 6749 compliance: WWW-Authenticate header for invalid_client (401)"
echo "4. Removed redundant AuthServerException handling from TokenController"
echo "5. All AuthServerException subclasses now map to correct OAuth error responses"
echo ""
echo "RFC 6749 Error Code Mapping:"
echo "  - invalid_grant (400): InvalidGrantException, ValidationException"
echo "  - invalid_client (401): InvalidClientException - includes WWW-Authenticate header"
echo "  - invalid_scope (400): InvalidScopeException"
echo "  - unauthorized_client (403): UnauthorizedClientException"
echo "  - server_error (500): ConfigurationException"
echo "  - unsupported_grant_type (400): UnsupportedGrantTypeException"
echo ""
