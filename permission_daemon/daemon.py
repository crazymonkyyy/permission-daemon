
import time
import os
import stat
import glob

def load_rules(config_path):
    rules = []
    with open(config_path, 'r') as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#'):
                parts = line.split()
                if len(parts) == 2:
                    rules.append((parts[0], parts[1]))
    print(f"Loaded {len(rules)} rules.")
    return rules

def parse_perms(perms_str):
    mode = 0
    if 'r' in perms_str:
        mode |= stat.S_IRUSR | stat.S_IRGRP | stat.S_IROTH
    if 'w' in perms_str:
        mode |= stat.S_IWUSR | stat.S_IWGRP
    if 'x' in perms_str:
        mode |= stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH
    return mode

def apply_permissions(file_path, rules):
    for pattern, perms in rules:
        if glob.fnmatch.fnmatch(os.path.basename(file_path), pattern):
            try:
                mode = parse_perms(perms)
                os.chmod(file_path, mode)
                print(f"Applied permissions {perms} to {file_path}")
            except Exception as e:
                print(f"Error applying permissions to {file_path}: {e}")

def main():
    config_path = 'permissions.conf'
    path = '.'  # Watch the current directory

    # Create a default config file if it doesn't exist
    if not os.path.exists(config_path):
        with open(config_path, 'w') as f:
            f.write("# Add your permission rules here.\n")
            f.write("# For example:\n")
            f.write("# *.log r\n")
            f.write("# *.tmp rw\n")
            f.write("*.py r\n")

    rules = load_rules(config_path)
    # Store the state of the files
    watched_files = {}
    for root, _, files in os.walk(path):
        for name in files:
            file_path = os.path.join(root, name)
            try:
                watched_files[file_path] = os.path.getmtime(file_path)
            except OSError:
                continue

    print(f"Watching directory: {path}")

    try:
        while True:
            time.sleep(1)
            # Check for modifications and new files
            for root, _, files in os.walk(path):
                for name in files:
                    file_path = os.path.join(root, name)
                    if file_path == config_path:
                        if file_path not in watched_files or \
                           os.path.getmtime(file_path) > watched_files[file_path]:
                            rules = load_rules(config_path)
                            watched_files[file_path] = os.path.getmtime(file_path)

                    if file_path not in watched_files:
                        print(f"New file detected: {file_path}")
                        apply_permissions(file_path, rules)
                        watched_files[file_path] = os.path.getmtime(file_path)
                    else:
                        try:
                            mtime = os.path.getmtime(file_path)
                            if mtime > watched_files[file_path]:
                                print(f"File modified: {file_path}")
                                apply_permissions(file_path, rules)
                                watched_files[file_path] = mtime
                        except OSError:
                            # File might have been deleted
                            del watched_files[file_path]
                            print(f"File deleted: {file_path}")


    except KeyboardInterrupt:
        print("Stopping daemon.")

if __name__ == "__main__":
    main()

