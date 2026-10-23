"""Forward real Netcode UDP traffic through seeded delay/jitter/loss; retain actual datagram counts, never simulated client state."""
import argparse
import heapq
import json
from pathlib import Path
import random
import selectors
import socket
import time


def main():
    """Run one loopback socket per client, bound the delayed queue, and publish counters on an explicit stop file or deadline."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--server", type=int, required=True)
    parser.add_argument("--ports", type=int, nargs="+", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--stop-file", type=Path, required=True)
    parser.add_argument("--seconds", type=float, default=120)
    parser.add_argument("--delay-ms", type=float, default=80)
    parser.add_argument("--jitter-ms", type=float, default=30)
    parser.add_argument("--loss", type=float, default=.05)
    parser.add_argument("--blackout-start", type=float, default=-1)
    parser.add_argument("--blackout-seconds", type=float, default=0)
    parser.add_argument("--blackout-file", type=Path)
    args = parser.parse_args()
    assert 0 <= args.loss <= 1 and args.delay_ms >= 0 and 0 <= args.jitter_ms <= args.delay_ms
    assert 1 <= len(args.ports) <= 8 and len(set(args.ports)) == len(args.ports)
    selector = selectors.DefaultSelector()
    stats = []
    for index, port in enumerate(args.ports):
        endpoint = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        endpoint.bind(("127.0.0.1", port)); endpoint.setblocking(False)
        record = {"port": port, "seed": 415 + index, "receivedDatagrams": 0, "receivedBytes": 0,
                  "sentDatagrams": 0, "sentBytes": 0, "randomDrops": 0, "blackoutDrops": 0, "queueDrops": 0,
                  "clientToServerBytes": 0, "serverToClientBytes": 0, "socketResets": 0}
        stats.append(record)
        selector.register(endpoint, selectors.EVENT_READ, {"client": None, "rng": random.Random(415 + index), "record": record})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.with_suffix(".ready").write_text("loopback UDP impairment sockets bound\n", encoding="utf-8")
    started = time.monotonic(); pending = []; serial = 0; maximum_queue = 0; next_progress = 0
    try:
        while time.monotonic() - started < args.seconds and not args.stop_file.exists():
            now = time.monotonic()
            if now >= next_progress:
                # Keep failure diagnostics available even if the owning runner must terminate a stalled scenario.
                args.output.with_suffix('.progress.json').write_text(json.dumps(stats, indent=2), encoding='utf-8')
                next_progress = now + 2
            while pending and pending[0][0] <= now:
                _, _, endpoint, address, payload, record = heapq.heappop(pending)
                endpoint.sendto(payload, address); record["sentDatagrams"] += 1; record["sentBytes"] += len(payload)
            timeout = min(.05, max(0, pending[0][0] - now)) if pending else .05
            for key, _ in selector.select(timeout):
                entry = key.data; record = entry["record"]
                try:
                    payload, source = key.fileobj.recvfrom(65535)
                except ConnectionResetError:
                    # Windows can surface an ICMP response after a completed client closes its UDP socket.
                    record["socketResets"] += 1
                    continue
                except BlockingIOError:
                    continue
                server = ("127.0.0.1", args.server)
                if source == server:
                    target = entry["client"]
                    if target is None: continue
                    record["serverToClientBytes"] += len(payload)
                else:
                    if entry["client"] is not None and source != entry["client"]: continue
                    entry["client"] = source; target = server; record["clientToServerBytes"] += len(payload)
                record["receivedDatagrams"] += 1; record["receivedBytes"] += len(payload)
                elapsed = time.monotonic() - started
                if (args.blackout_file is not None and args.blackout_file.exists()) or (args.blackout_start >= 0 and args.blackout_start <= elapsed < args.blackout_start + args.blackout_seconds):
                    record["blackoutDrops"] += 1; continue
                if entry["rng"].random() < args.loss: record["randomDrops"] += 1; continue
                if len(pending) >= 4096: record["queueDrops"] += 1; continue
                delay = (args.delay_ms + entry["rng"].uniform(-args.jitter_ms, args.jitter_ms)) / 1000
                serial += 1; heapq.heappush(pending, (time.monotonic() + delay, serial, key.fileobj, target, payload, record))
                maximum_queue = max(maximum_queue, len(pending))
    finally:
        elapsed = time.monotonic() - started
        for key in list(selector.get_map().values()): key.fileobj.close()
        selector.close()
        result = {"seconds": elapsed, "configuredDelayMsPerDirection": args.delay_ms, "configuredJitterMs": args.jitter_ms,
                  "configuredRandomLoss": args.loss, "maximumQueuedDatagrams": maximum_queue, "pendingAtStop": len(pending),
                  "blackoutStartSeconds": args.blackout_start, "blackoutDurationSeconds": args.blackout_seconds, "clients": stats,
                  "blackoutFileControlUsed": args.blackout_file is not None,
                  "scope": "Actual loopback UDP datagrams including handshake, ACKs and retransmissions; excludes IP/UDP header bytes. Not WAN/capacity evidence."}
        args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
