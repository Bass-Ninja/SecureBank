#!/usr/bin/env bash
# Idempotently provisions the "bank-staff" group/role mapping and a
# "staff1" test user against an already-running Keycloak instance.
#
# securebank-realm.json is only imported by Keycloak the first time the
# "securebank" realm doesn't exist yet (see --import-realm). On a
# long-lived dev instance the realm already exists, so JSON changes are
# never re-applied by `docker compose up`. This script uses the Admin
# REST API instead, safe to re-run any time.
#
# Usage: ./seed-staff-users.sh
# Env overrides: KEYCLOAK_URL, REALM, ADMIN_USERNAME, ADMIN_PASSWORD,
#                STAFF_USERNAME, STAFF_PASSWORD, STAFF_ROLE, STAFF_GROUP

set -euo pipefail

KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8081}"
REALM="${REALM:-securebank}"
ADMIN_USERNAME="${ADMIN_USERNAME:-admin}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-admin_dev_password}"
STAFF_USERNAME="${STAFF_USERNAME:-staff1}"
STAFF_PASSWORD="${STAFF_PASSWORD:-SecureBank123!}"
STAFF_ROLE="${STAFF_ROLE:-support}"
STAFF_GROUP="${STAFF_GROUP:-bank-staff}"

admin_token() {
  curl -s -X POST "$KEYCLOAK_URL/realms/master/protocol/openid-connect/token" \
    -d "client_id=admin-cli" \
    -d "username=$ADMIN_USERNAME" \
    -d "password=$ADMIN_PASSWORD" \
    -d "grant_type=password" \
    | python3 -c "import json,sys; print(json.load(sys.stdin)['access_token'])"
}

TOKEN=$(admin_token)
AUTH_HEADER="Authorization: Bearer $TOKEN"
API="$KEYCLOAK_URL/admin/realms/$REALM"

echo "== Role: $STAFF_ROLE =="
ROLE_JSON=$(curl -s "$API/roles/$STAFF_ROLE" -H "$AUTH_HEADER")
ROLE_ID=$(echo "$ROLE_JSON" | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")
echo "role id: $ROLE_ID"

echo "== Group: $STAFF_GROUP =="
GROUP_ID=$(curl -s "$API/groups?search=$STAFF_GROUP" -H "$AUTH_HEADER" \
  | python3 -c "
import json, sys
groups = json.load(sys.stdin)
match = [g for g in groups if g['name'] == '$STAFF_GROUP']
print(match[0]['id'] if match else '')
")

if [ -z "$GROUP_ID" ]; then
  echo "creating group..."
  curl -s -o /dev/null -w "HTTP %{http_code}\n" -X POST "$API/groups" \
    -H "$AUTH_HEADER" -H "Content-Type: application/json" \
    -d "{\"name\": \"$STAFF_GROUP\"}"

  GROUP_ID=$(curl -s "$API/groups?search=$STAFF_GROUP" -H "$AUTH_HEADER" \
    | python3 -c "import json,sys; print(json.load(sys.stdin)[0]['id'])")
else
  echo "already exists."
fi
echo "group id: $GROUP_ID"

echo "== Role mapping: $STAFF_GROUP -> $STAFF_ROLE =="
HAS_ROLE=$(curl -s "$API/groups/$GROUP_ID/role-mappings/realm" -H "$AUTH_HEADER" \
  | python3 -c "
import json, sys
roles = json.load(sys.stdin)
print('yes' if any(r['name'] == '$STAFF_ROLE' for r in roles) else '')
")

if [ -z "$HAS_ROLE" ]; then
  echo "mapping role onto group..."
  curl -s -o /dev/null -w "HTTP %{http_code}\n" -X POST "$API/groups/$GROUP_ID/role-mappings/realm" \
    -H "$AUTH_HEADER" -H "Content-Type: application/json" \
    -d "[{\"id\": \"$ROLE_ID\", \"name\": \"$STAFF_ROLE\"}]"
else
  echo "already mapped."
fi

echo "== User: $STAFF_USERNAME =="
USER_ID=$(curl -s "$API/users?username=$STAFF_USERNAME&exact=true" -H "$AUTH_HEADER" \
  | python3 -c "
import json, sys
users = json.load(sys.stdin)
print(users[0]['id'] if users else '')
")

if [ -z "$USER_ID" ]; then
  echo "creating user..."
  curl -s -o /dev/null -w "HTTP %{http_code}\n" -X POST "$API/users" \
    -H "$AUTH_HEADER" -H "Content-Type: application/json" \
    -d "{
      \"username\": \"$STAFF_USERNAME\",
      \"firstName\": \"Staff\",
      \"lastName\": \"SecureBank\",
      \"email\": \"$STAFF_USERNAME@securebank.local\",
      \"emailVerified\": true,
      \"enabled\": true,
      \"credentials\": [{\"type\": \"password\", \"value\": \"$STAFF_PASSWORD\", \"temporary\": false}]
    }"

  USER_ID=$(curl -s "$API/users?username=$STAFF_USERNAME&exact=true" -H "$AUTH_HEADER" \
    | python3 -c "import json,sys; print(json.load(sys.stdin)[0]['id'])")
else
  echo "already exists."
fi
echo "user id: $USER_ID"

echo "== Group membership: $STAFF_USERNAME -> $STAFF_GROUP =="
IN_GROUP=$(curl -s "$API/users/$USER_ID/groups" -H "$AUTH_HEADER" \
  | python3 -c "
import json, sys
groups = json.load(sys.stdin)
print('yes' if any(g['name'] == '$STAFF_GROUP' for g in groups) else '')
")

if [ -z "$IN_GROUP" ]; then
  echo "adding user to group..."
  curl -s -o /dev/null -w "HTTP %{http_code}\n" -X PUT "$API/users/$USER_ID/groups/$GROUP_ID" \
    -H "$AUTH_HEADER"
else
  echo "already a member."
fi

echo "Done."
