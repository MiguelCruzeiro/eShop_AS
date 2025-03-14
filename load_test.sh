#!/bin/bash

API_URL="http://localhost:5222/api/catalog/items"
API_VERSION="1.0"

simulate_user() {
  while true; do
    ITEM_ID=$((RANDOM % 100 + 1))  # Random item ID between 1 and 100
    RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "${API_URL}/${ITEM_ID}?api-version=${API_VERSION}")
    echo "User requested item ${ITEM_ID} - Response: ${RESPONSE}"
    sleep $((RANDOM % 3 + 1))  # Random delay (1-3 seconds)
  done
}

# Start 10 concurrent "users"
for i in {1..10}; do
  simulate_user &
done

wait  # Keep script running
