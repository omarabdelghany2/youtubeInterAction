import requests
import json
import time
import os
import logging
import atexit

from flask import Flask
from flask_socketio import SocketIO
import socketio as psio
from signalrcore.hub_connection_builder import HubConnectionBuilder

app = Flask(__name__)
socketio = SocketIO(app, cors_allowed_origins="*")

# Global client
sio = psio.Client()

# Your .NET API base URL
API_BASE_URL = "http://localhost:5050/api"
SIGNALR_URL = "http://localhost:5050/youtubechathub"

# --- Streamlabs Handling ---

def get_streamlabs_token():
    try:
        response = requests.get(f"{API_BASE_URL}/streamlabs/token")
        response.raise_for_status()
        return response.json().get("token")
    except Exception as e:
        print(f"Failed to fetch Streamlabs token: {e}")
        return None

def add_watcher(username, interaction):
    try:
        response = requests.post(
            f"{API_BASE_URL}/watchers",
            json={
                "username": username,
                "interaction": interaction,
                "platform": "YouTube"
            }
        )
        if response.status_code == 201:
            print(f"New watcher added: {username} with interaction: {interaction}")
            return True
        elif response.status_code == 409:
            print(f"Watcher already exists: {username}")
            return False
        else:
            print(f"Unexpected response: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"Error adding watcher: {e}")
        return False

def broadcast_watchers():
    try:
        response = requests.post(f"{API_BASE_URL}/watchers/broadcast")
        if response.status_code == 200:
            print("Broadcast sent successfully.")
        else:
            print(f"Failed to broadcast: {response.status_code}")
    except Exception as e:
        print(f"Error broadcasting watchers: {e}")

def attach_streamlabs_handlers(client):
    @client.event
    def connect():
        print("Connected to Streamlabs WebSocket.")

    @client.event
    def disconnect():
        print("Disconnected from Streamlabs WebSocket. Will reconnect.")

    @client.event
    def connect_error(data):
        print("Streamlabs connection error. Retrying in 20 seconds...")
        time.sleep(20)
        reconnect_to_streamlabs()

    @client.event
    def event(data):
        print("Event received.")
        try:
            message = json.loads(data) if isinstance(data, str) else data
            print("Full event message:", message)

            event_type = message.get("type")
            print("Event type:", event_type)

            if event_type == "donation":
                donation = message.get("message", [{}])[0]
                username = donation.get("from")
                if username:
                    is_new = add_watcher(username, interaction="tip")
                    if is_new:
                        broadcast_watchers()

            elif event_type == "follow":
                follow_data = message.get("message", [{}])[0]
                username = follow_data.get("name")
                if username:
                    is_new = add_watcher(username, interaction="subscription")
                    if is_new:
                        broadcast_watchers()

            elif event_type == "superchat":
                sc_data = message.get("message", [{}])[0]
                username = sc_data.get("name")
                if username:
                    is_new = add_watcher(username, interaction="superchat")
                    if is_new:
                        broadcast_watchers()

            elif event_type == "membershipGift":
                gift = message.get("message", [{}])[0]
                username = gift.get("name")
                if username:
                    is_new = add_watcher(username, interaction="membershipGift")
                    if is_new:
                        broadcast_watchers()

            else:
                print("Unhandled event type:", event_type)

        except Exception as e:
            print(f"Error handling event: {e}")

def reconnect_to_streamlabs():
    token = get_streamlabs_token()
    if token:
        try:
            sio.connect(f"https://sockets.streamlabs.com?token={token}")
        except Exception as e:
            print(f"Reconnect failed: {e}")
            time.sleep(20)
            reconnect_to_streamlabs()
    else:
        print("Token unavailable; skipping reconnect.")

# --- SignalR Integration ---

hub_connection = HubConnectionBuilder()\
    .with_url(SIGNALR_URL)\
    .configure_logging(logging.INFO)\
    .build()

def on_streamlabs_token_updated(args):
    print("SignalR: Streamlabs token updated. Reconnecting to Streamlabs.")
    global sio
    try:
        if sio.connected:
            try:
                sio.disconnect()
            except Exception as e:
                print(f"Warning: Error during Streamlabs disconnect: {e}")
        else:
            print("Streamlabs client not connected. Skipping disconnect.")

        # Recreate client and reattach handlers
        sio = psio.Client()
        attach_streamlabs_handlers(sio)
        reconnect_to_streamlabs()

    except Exception as e:
        print(f"Unexpected error in token refresh: {e}")

hub_connection.on("StreamlabsTokenUpdated", on_streamlabs_token_updated)

@atexit.register
def shutdown():
    print("Shutting down...")
    try:
        if hub_connection:
            hub_connection.stop()
        if sio.connected:
            sio.disconnect()
    except Exception as e:
        print(f"Shutdown error: {e}")

# --- Main Entry ---

if __name__ == "__main__":
    attach_streamlabs_handlers(sio)
    reconnect_to_streamlabs()
    hub_connection.start()
    socketio.run(app, host="0.0.0.0", port=5001)
