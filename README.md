
# bowInstaller: a tool that makes it easy to install Bash on Windows

**bowInstaller** makes it easy the installation of WSL (Windows Subsystem for Linux)

The installer will:

 * Enable developer mode (required)
 * Enable WSL feature
 * Install Bash on Windows
 * Optional: create a default user/password
 * Optional: run a post-installation script

> Note: in order to enable WSL feature you will need to reboot your computer
> during the installation

## GUI Wizard

> Work in progress: currently not available

You can install Bash on Windows using the wizard:

```
bowinstaller.msi
```

<!--
# TODO: add a screenshot
-->

## CLI for automated installations

A command-line interface is also available, specially useful if you need to install Bash on Windows in an automated way:

### Usage

```
Usage: bowinstaller.exe <options>

  -v, --verbose        (Default: False) Prints all messages to standard output.
  -u, --user           Default user.
  -p, --password       Default user's password
  -y, --assumeyes      (Default: False) Assume yes. Attention: computer will be
                       restarted
  -n, --noreboot       (Default: False) Do not reboot automatically
  -r, --resume         (Default: False) Resume installation after the reboot
  -p, --postinstall    Batch script to run after the installation
  -d, --uninstall      (Default: False) Uninstall Bash on Windows
  --version            (Default: False) Prints version information to standard
                       output
  --help               Display this help screen.
```

If no user is specified, `root` will be the default user (with no password).

### Examples

```bat
# Interactive installation (cannot specify default user):
bowinstaller.exe

# Automated installation:
bowinstaller.exe --user myuser --password mypassword --postinstall C:\myscript.bat
```
