#!/usr/bin/env python3
"""Sine signal TCP master for Raspberry Pi Zero W.

Master = TCP server. A slave connects over IP:port.
Master continuously sends 5 messages (one signal each), where each signal value
is A * sin(2*pi*f*t + phi). Master also listens for commands that change the
amplitude (A), frequency (f) and optionally phase (phi) of any signal at runtime.

Wire protocol (newline-delimited JSON):
  Master -> slave:  {"name": "sig1", "value": 0.7071, "t": 0.05}\n   (5x per cycle)
  Slave  -> master: {"cmd": "set", "name": "sig1", "A": 2.0, "f": 5.0, "phi": 0.0}\n
                    (A / f / phi are all optional; only the provided ones change)
"""
import socket
import threading
import json
import math
import time

HOST = "0.0.0.0"
PORT = 5000
SEND_HZ = 200            # how often a full set of samples is sent, per second

# Per-signal initial parameters.
#   A   = amplitude
#   f   = frequency [Hz]
#   phi = phase offset [rad]
# The send order follows the order of keys below (dict preserves insertion order).
INITIAL_PARAMS = {
    "sine1": {"A": 10.0, "O": 2.0,  "f": 1.0,  "phi": 0.0},
    "sine2": {"A": 20.0, "O": -5.0,  "f": 0.5,  "phi": math.pi / 4},
    "sine3": {"A": 5.0,  "O": 8.0,  "f": 10.0,  "phi": math.pi / 2},
    "sine4": {"A": 30.0, "O": 0.5,  "f": 0.25, "phi": math.pi},
    "sine5": {"A": 15.0, "O": 2.0,  "f": 2.0,  "phi": 3 * math.pi / 2},
}
SIGNAL_NAMES = list(INITIAL_PARAMS.keys())


class SignalStore:
    """Thread-safe store of per-signal parameters."""

    def __init__(self, initial):
        self._lock = threading.Lock()
        # deep-ish copy so INITIAL_PARAMS is not mutated by runtime commands
        self._sig = {n: dict(p) for n, p in initial.items()}

    def value(self, name, t):
        with self._lock:
            p = self._sig[name]
            A, O, f, phi = p["A"], p["O"], p["f"], p["phi"]
        return O + A * math.sin(2.0 * math.pi * f * t + phi)

    def set_param(self, name, A=None, O=None, f=None, phi=None):
        with self._lock:
            if name not in self._sig:
                return False
            if A is not None:
                self._sig[name]["A"] = float(A)
            if O is not None:
                self._sig[name]["O"] = float(O)
            if f is not None:
                self._sig[name]["f"] = float(f)
            if phi is not None:
                self._sig[name]["phi"] = float(phi)
            return True


def reader_thread(conn, store, stop):
    """Listen for commands from the slave and apply parameter changes."""
    conn_file = conn.makefile("rb")
    try:
        for raw in conn_file:                 # read line by line
            if stop.is_set():
                break
            line = raw.strip()
            if not line:
                continue
            try:
                msg = json.loads(line.decode("utf-8"))
            except json.JSONDecodeError:
                continue
            if msg.get("cmd") == "set":
                store.set_param(
                    msg.get("name"),
                    msg.get("A"),
		    msg.get("O"),
                    msg.get("f"),
                    msg.get("phi"),
                )
    except OSError:
        pass
    finally:
        stop.set()


def handle_client(conn, addr, store):
    print(f"Slave connected: {addr}")
    stop = threading.Event()
    t = threading.Thread(target=reader_thread,
                         args=(conn, store, stop), daemon=True)
    t.start()

    t0 = time.monotonic()
    period = 1.0 / SEND_HZ
    try:
        while not stop.is_set():
            now = time.monotonic() - t0
            for name in SIGNAL_NAMES:                  # 5 messages, one signal each
                msg = {"name": name,
                       "value": store.value(name, now),
                       "t": now}
                conn.sendall((json.dumps(msg) + "\n").encode("utf-8"))
            time.sleep(period)
    except OSError:
        pass
    finally:
        stop.set()
        conn.close()
        print(f"Slave disconnected: {addr}")


def main():
    store = SignalStore(INITIAL_PARAMS)
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind((HOST, PORT))
    srv.listen(1)
    print(f"Master listening on {HOST}:{PORT}")
    try:
        while True:
            conn, addr = srv.accept()
            # serial handling of one slave at a time; wrap in a thread for many
            handle_client(conn, addr, store)
    except KeyboardInterrupt:
        pass
    finally:
        srv.close()


if __name__ == "__main__":
    main()