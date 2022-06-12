#!/usr/bin/env bash

### Functions ###

# Announces something on the terminal
# Usage: announce 'message'
announce()
{
    printf '\n.::%s::.\n' "$*"
}

# Prompts the user to type something
# Usage: get_input 'Input message'
get_input()
{
    read -p "$*" version
    echo $version
}

# Builds the bot
# Usage: build_akko $location $architecture
build_akko()
{
    if [[ ! -d $1 ]]; then
        printf 'Could not find the bot at "%s"\n' $1 >&2
        exit 3
    fi

    printf '\n> Building Akko for %s\n' $2
    dotnet publish -c Release $1 -r $2 -p:PublishSingleFile=true --self-contained --nologo
}

# Builds a cog
# Usage: build_cog $location
build_cog()
{
    if [[ ! -d $1 ]]; then
        printf 'Could not find the cog at "%s"\n' $1 >&2
        exit 3
    fi

    printf '\n> Building cog "%s"\n' $(basename $1)
    dotnet publish -c Release $1 --nologo
}

# Cleans up a publish folder
# Usage: cleanup_publish $publish_dir
cleanup_publish()
{
    if [[ ! -d $1 ]]; then
        printf 'Could not find the publish folder at "%s"\n' $1 >&2
        exit 3
    fi

    # Check if glob patterns are not enabled
    if [[ $(shopt extglob) =~ "off" ]]; then
        local glob='true'
        shopt -s extglob
    fi

    # Write the final binary to the publish root folder
    mv -f $1/publish/* $1

    # Delete all compilation files except the binary we 
    # just moved and the Data folder
    local pattern="$1/!(AkkoBot.exe|AkkoBot|Data)" # This variable is needed because Bash whines otherwise
    rm -rf $pattern

    # Disable glob patterns if it was disabled previously
    if [[ $glob == 'true' ]]; then
        shopt -u extglob
    fi
}

# Creates the publish zip packs
# Usage: create_pack $bot_version $publish_path $publish_name $pack_tag
# Remarks: $pack_tag is optional
create_pack()
{
    if [[ ! -d "$2/$3" ]]; then
        printf 'Could not find the publish folder at "%s"\n' "$2/$3" >&2
        exit 3
    fi

    # If function has 3 or less parameters or pack_tag is unset/empty
    # set local variable to $3, else set to $4
    local pack_name=$( (( $# <= 3 )) || [[ -z $4 ]] && echo $3 || echo $4 )

    # Create the .zip file
    # We need to juggle with pushd because zip likes to add junk
    # directories to the resulting file
    pushd "$2/$3" > /dev/null # Silence the printing of the directory stack
    zip -rq9 "../AkkoBot_$1_$pack_name.zip" ./*
    popd > /dev/null
}


### Main ###

# Variables
declare -r BOT_VER=$(get_input 'Type the version of the bot (eg. 0.3.5-beta): ')
declare -r AKKO_DIR='./AkkoBot'
declare -r PUBLISH_DIR="$AKKO_DIR/bin/Release/net6.0"
declare -r COG_DIRS=('./AkkoCog.AntiPhishing' './AkkoCog.DangerousCommands')
declare -r ARCHS=('win-x64' 'linux-x64' 'linux-arm64' 'osx-x64' 'osx-arm64')

# Clean up the publish folder
announce 'Cleaning up the publish folder'
printf 'Removed %d directories and files\n' $(echo "$(tree $PUBLISH_DIR -a | wc -l) - 3" | bc)
rm -rf $PUBLISH_DIR

# Build the bot for multiple platforms
announce 'Building Akko'

for arch in "${ARCHS[@]}"
do
    build_akko $AKKO_DIR $arch
done

# Build the cogs
announce 'Building Cogs'

for cog_dir in "${COG_DIRS[@]}"
do
    build_cog $cog_dir
done

# Clean up the publish folders
announce 'Preparing the publish packages'

for arch in "${ARCHS[@]}"
do
    cleanup_publish "$PUBLISH_DIR/$arch"
    create_pack $BOT_VER $PUBLISH_DIR $arch
    echo -e "- Packaged $arch"
done

# Create cogs package
echo 'To install the cogs, just drop the "Cogs" directory inside the "Data" directory.' > "$PUBLISH_DIR/Data/readme.txt"
create_pack $BOT_VER $PUBLISH_DIR 'Data' 'cogs'
echo -e '- Packaged cogs\n'

# End
echo -e "Packages published successfully at:\n$PUBLISH_DIR\n"