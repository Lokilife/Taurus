#!/usr/bin/env bash
set -euo pipefail
repo_root=$(cd "$(dirname "$0")/.." && pwd)
nuget_root=${NUGET_PACKAGES:-"$HOME/.nuget/packages"}
default_protoc="$nuget_root/grpc.tools/2.76.0/tools/linux_x64/protoc"
protoc_path=${TAURUS_PROTOC:-"$default_protoc"}
plugin_root="$repo_root/.tools"
output_dir="$repo_root/src/Taurus.WebClient/src/lib/generated"
mkdir -p "$plugin_root" "$output_dir"
if [ ! -x "$plugin_root/protoc-gen-grpc-web" ]; then
    grpc_release=https://github.com/grpc/grpc-web/releases/download/2.0.2
    grpc_plugin=protoc-gen-grpc-web-2.0.2-linux-x86_64
    curl -fsSL "$grpc_release/$grpc_plugin" \
        -o "$plugin_root/protoc-gen-grpc-web"
    printf '%s  %s\n' \
        10ff6c6e58018ff9e684cff1d9c008b8cc79d915c4f8be4fd47791333e1be299 \
        "$plugin_root/protoc-gen-grpc-web" | sha256sum -c -
    chmod +x "$plugin_root/protoc-gen-grpc-web"
fi
# protoc's JavaScript generator is distributed separately from current protoc.
if [ ! -x "$plugin_root/protoc-gen-js" ]; then
    js_project=https://github.com/protocolbuffers/protobuf-javascript
    js_archive=protobuf-javascript-3.21.4-linux-x86_64.tar.gz
    curl -fsSL "$js_project/releases/download/v3.21.4/$js_archive" \
        -o "$plugin_root/js.tar.gz"
    printf '%s  %s\n' \
        c57ba4130471c642462fcf98c844a3c933f6c4708b9fddc859900fd2a2e72a45 \
        "$plugin_root/js.tar.gz" | sha256sum -c -
    tar -xzf "$plugin_root/js.tar.gz" -C "$plugin_root"
    chmod +x "$plugin_root/bin/protoc-gen-js"
    ln -sf bin/protoc-gen-js "$plugin_root/protoc-gen-js"
fi
"$protoc_path" -I "$repo_root/src/Taurus.Shared/Protos" \
    --plugin="protoc-gen-js=$plugin_root/protoc-gen-js" \
    --plugin="protoc-gen-grpc-web=$plugin_root/protoc-gen-grpc-web" \
    --js_out="import_style=commonjs,binary:$output_dir" \
    --grpc-web_out="import_style=typescript,mode=grpcweb:$output_dir" \
    "$repo_root/src/Taurus.Shared/Protos/authentication.proto" \
    "$repo_root/src/Taurus.Shared/Protos/user.proto"
# Node tools need the generated CommonJS module to have an explicit module type.
printf '%s\n' '{"type":"commonjs"}' > "$output_dir/package.json"
