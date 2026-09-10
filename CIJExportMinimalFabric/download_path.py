import ntpath
import os


def canonical_download_root(download_path):
    if not download_path:
        raise ValueError("LOCAL_DOWNLOAD_PATH must be set")

    return os.path.realpath(os.path.abspath(download_path))


def local_download_path(download_root, remote_path):
    if not isinstance(remote_path, str) or not remote_path:
        raise ValueError("OneLake path must be a non-empty string")
    if "\\" in remote_path:
        raise ValueError(f"Unsafe OneLake path: {remote_path!r}")

    drive, _ = ntpath.splitdrive(remote_path)
    parts = remote_path.split("/")
    if (
        drive
        or remote_path.startswith("/")
        or any(part in ("", ".", "..") or ntpath.splitdrive(part)[0] for part in parts)
    ):
        raise ValueError(f"Unsafe OneLake path: {remote_path!r}")

    destination = os.path.realpath(os.path.join(download_root, *parts))
    try:
        common_path = os.path.commonpath((download_root, destination))
    except ValueError:
        raise ValueError(f"OneLake path escapes download root: {remote_path!r}") from None

    if os.path.normcase(common_path) != os.path.normcase(download_root):
        raise ValueError(f"OneLake path escapes download root: {remote_path!r}")

    return destination
