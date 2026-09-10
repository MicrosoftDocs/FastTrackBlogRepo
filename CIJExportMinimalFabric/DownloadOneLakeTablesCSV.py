import dotenv
import os
from azure.identity import DefaultAzureCredential
from azure.storage.filedatalake import DataLakeServiceClient
from download_path import canonical_download_root, local_download_path

# Configuration
dotenv.load_dotenv()
account_url = os.environ.get("ACCOUNT_URL")
workspace_name = os.environ.get("WORKSPACE_NAME")
lakehouse_name = os.environ.get("LAKEHOUSE_NAME")
download_root = canonical_download_root(os.environ.get("LOCAL_DOWNLOAD_PATH"))

data_path = lakehouse_name + ".lakehouse/Tables"


# Authenticate

credential = DefaultAzureCredential()
service_client = DataLakeServiceClient(account_url=account_url, credential=credential)

# Connect to the lakehouse filesystem
filesystem_client = service_client.get_file_system_client(workspace_name)

# List tables (folders)
paths = filesystem_client.get_paths(path=data_path)
table_folders = [p.name for p in paths if p.is_directory]
filecount = 0
# Iterate through tables
for table in table_folders:
    
    #print(f"Processing table: {table}")
    # Get directory client
    directory_client = filesystem_client.get_directory_client(table)
    files = directory_client.get_paths()

    # Download each file
    for file in files:
        download_path = local_download_path(download_root, file.name)
        if not file.is_directory:
            file_client = filesystem_client.get_file_client(file.name)
            os.makedirs(os.path.dirname(download_path), exist_ok=True)
            with open(download_path, "wb") as f:
                download = file_client.download_file()
                f.write(download.readall())
            #print(f"Downloaded: {file.name} → {download_path}")
            filecount += 1

    print(f"downloaded {filecount} files from {table} ")
    filecount = 0
        
 