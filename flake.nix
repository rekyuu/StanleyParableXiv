{
    inputs = {
        nixpkgs.url = "github:nixos/nixpkgs/nixos-24.11";
    };

    outputs = { self, nixpkgs }:
        let
            pkgs = nixpkgs.legacyPackages.x86_64-linux;
            dotnet = pkgs.dotnetCorePackages.sdk_9_0;
        in {
            devShells.x86_64-linux.default = pkgs.mkShell {
                nativeBuildInputs = with pkgs; [
                    dotnet
                ];
            };
        };
}
