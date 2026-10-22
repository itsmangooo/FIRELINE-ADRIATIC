#!/bin/sh
# Execute the Linux player in an ignored workspace-local Ubuntu Base root under the existing WSL kernel.
# No Docker daemon, new WSL distribution, system package install or public listen socket.
set -eu
project=${1:?Pass the WSL path to the checkout}
runtime="$project/Builds/LinuxRuntime/rootfs"
archive="$project/Builds/LinuxRuntime/ubuntu-base-24.04.5-base-amd64.tar.gz"
expected=e77b6f10c2590cef872b33ee9f635a0e3fd1f57fb074c0e52b5c7f56147a0c86
printf '%s  %s\n' "$expected" "$archive" | sha256sum -c -
if [ ! -f "$runtime/.fireline-root-ready" ]; then
    mkdir -p "$runtime"
    tar -xzf "$archive" -C "$runtime"
    touch "$runtime/.fireline-root-ready"
fi
if [ "${2:-}" = "--prepare" ]; then
    chroot "$runtime" /bin/sh -c 'cat /etc/os-release; /lib/x86_64-linux-gnu/libc.so.6 | head -1'
    exit 0
fi
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
chroot "$runtime" /usr/bin/ldd /workspace/FirelineServer.x86_64
chroot "$runtime" /bin/sh -c 'cat /etc/os-release; /lib/x86_64-linux-gnu/libc.so.6 | head -1'
# Negative case deliberately requires a client and must return the documented smoke-failure exit.
set +e
timeout 90 chroot "$runtime" /workspace/FirelineServer.x86_64 -batchmode -nographics -logFile /workspace/linux-negative.log \
    --fireline-bind 127.0.0.1 --fireline-port 7991 --fireline-smoke-seconds 5 --fireline-min-clients 1
result=$?
set -e
test "$result" -eq 4
grep '^FIRELINE_' "$project/Builds/LinuxServer/linux-negative.log"
grep -q 'FIRELINE_SERVER_HEADLESS valid=True' "$project/Builds/LinuxServer/linux-negative.log"
grep -q 'FIRELINE_SERVER_SMOKE passed=False' "$project/Builds/LinuxServer/linux-negative.log"
echo "LINUX_NEGATIVE_PASSED expectedExit=4"
