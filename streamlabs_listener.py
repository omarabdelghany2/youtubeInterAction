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
sio = psio.Client()

# Your .NET API base URL
API_BASE_URL = "http://localhost:5050/api"
SIGNALR_URL = "http://localhost:5050/youtubechathub"

# --- Streamlabs handling ---

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

@sio.event
def connect():
    print("Connected to Streamlabs WebSocket.")

@sio.event
def disconnect():
    print("Disconnected from Streamlabs WebSocket. Will reconnect.")

@sio.event
def connect_error(data):
    print("Streamlabs connection error. Retrying in 20 seconds...")
    time.sleep(20)
    reconnect_to_streamlabs()

@sio.event
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
            follow_data = message.get("message", [{}])[0]
            username = follow_data.get("name")
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
    #token = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ0b2tlbiI6IkE4QUQxNEJCM0NCMjE0RUZGMzY3MEJCQjg4QTA5RkNGOEE2Qjk5MDhFMjNGRTEzMzQ0QjEzRDQzRDMyMDQ5MEZBRDZDOTMzMTkwODAyN0IxMDAyRkFCQjczMzI3NjcyQTI2NDY3NDM3NTZDMjA0QUQ0QUQyRUM2MEMxNjE4MjcwQzFGNEQyNkY4N0EyNjg1MTA3Q0RBOTUxQzM0RjkwNDM5MDZGMzJCMTI5NTFFQjI5NzMwMkU1OTBGQ0I3NDFCMzUxOUQyNjU2NkU0RUIwQ0UyRDdCQURGQTNGNEZGRTEyRDVGRUNBN0E4NzQwNjcxMEE2QUVBREQ5NkMiLCJyZWFkX29ubHkiOnRydWUsInByZXZlbnRfbWFzdGVyIjp0cnVlLCJ5b3V0dWJlX2lkIjoiVUNXOU16cWFWUHJ1UmR4OVUzbGk5NmVRIn0.jBrpglw12uVZqaiAq0XXG83U3WvedZls3WpoY3HZVlo"
    if token:
        try:
            sio.connect(f"https://sockets.streamlabs.com?token={token}")
        except Exception as e:
            print(f"Reconnect failed: {e}")
            time.sleep(20)
            reconnect_to_streamlabs()
    else:
        print("Token unavailable; skipping reconnect.")

# --- SignalR integration ---

hub_connection = HubConnectionBuilder()\
    .with_url(SIGNALR_URL)\
    .configure_logging(logging.INFO)\
    .build()

def on_streamlabs_token_updated(args):
    print("SignalR: Streamlabs token updated. Reconnecting to Streamlabs.")
    try:
        if sio.connected:
            sio.disconnect()
    except Exception as e:
        print(f"Error during Streamlabs disconnect: {e}")
    reconnect_to_streamlabs()

hub_connection.on("StreamlabsTokenUpdated", on_streamlabs_token_updated)

@atexit.register
def shutdown():
    print("Shutting down...")
    if hub_connection:
        hub_connection.stop()
    if sio.connected:
        sio.disconnect()

# --- Main entry ---

if __name__ == "__main__":
    reconnect_to_streamlabs()
    hub_connection.start()
    socketio.run(app, host="0.0.0.0", port=5001)
