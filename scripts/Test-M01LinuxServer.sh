#!/bin/sh
# Reuse the already-verified workspace Ubuntu root; no Docker daemon or distro installation.
set -eu
project=${1:?Pass the WSL path to the checkout}
runtime="$project/Builds/LinuxRuntime/rootfs"
test -f "$runtime/.fireline-root-ready"
test -f "$project/Builds/LinuxServer/FirelineServer.x86_64"
mkdir -p "$runtime/workspace" "$runtime/proc"
mount --bind "$project/Builds/LinuxServer" "$runtime/workspace"
cleanup() {
    umount "$runtime/proc" 2>/dev/null || true
    for device in null zero random urandom; do umount "$runtime/dev/$device" 2>/dev/null || true; done
    umount "$runtime/workspace"
}
trap cleanup EXIT
mount -t proc proc "$runtime/proc"
for device in null zero random urandom; do
    touch "$runtime/dev/$device"
    mount --bind "/dev/$device" "$runtime/dev/$device"
done
for iteration in 1 2; do
    timeout 300 chroot "$runtime" /workspace/FirelineServer.x86_64 -batchmode -nographics \
        -logFile "/workspace/m01-proof-$iteration.log" --fireline-bind 127.0.0.1 --fireline-port 7993 \
        --fireline-sim-proof "/workspace/m01-proof-$iteration.json" \
        > "$project/Builds/LinuxServer/m01-proof-$iteration-stdout.log" 2>&1
    grep '^FIRELINE_' "$project/Builds/LinuxServer/m01-proof-$iteration.log"
    grep -q 'FIRELINE_SIMULATION_PROOF passed=True headless=True cases=9' "$project/Builds/LinuxServer/m01-proof-$iteration.log"
done
echo 'M01_LINUX_PROOF_PASSED processes=2'
