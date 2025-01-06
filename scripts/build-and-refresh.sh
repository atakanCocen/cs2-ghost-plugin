export PLUGIN_NAME=GhostPlugin
export PLUGIN_PATH="$CS2_SERVER_PATH/addons/counterstrikesharp/plugins/$PLUGIN_NAME/"

dotnet build
mkdir -p $PLUGIN_PATH
cp bin/Debug/net8.0/*.* $PLUGIN_PATH
