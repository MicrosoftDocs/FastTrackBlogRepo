import dotenv
import os
from azure.identity import DefaultAzureCredential
from azure.storage.filedatalake import DataLakeServiceClient

# Configuration
dotenv.load_dotenv()
account_url = os.environ.get("ACCOUNT_URL")
workspace_name = os.environ.get("WORKSPACE_NAME")
lakehouse_name = os.environ.get("LAKEHOUSE_NAME")
local_download_path = os.environ.get("LOCAL_DOWNLOAD_PATH")

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
    table_path = os.path.join(local_download_path, table)
    os.makedirs(table_path, exist_ok=True)

    # Get directory client
    directory_client = filesystem_client.get_directory_client(table)
    files = directory_client.get_paths()

    # Download each file
    for file in files:
        if not file.is_directory:
            file_client = filesystem_client.get_file_client(file.name)
            download_path = os.path.join(table_path, os.path.basename(file.name))
            with open(download_path, "wb") as f:
                download = file_client.download_file()
                f.write(download.readall())
            #print(f"Downloaded: {file.name} → {download_path}")
            filecount += 1
        else: 
            os.makedirs(file.name, exist_ok=True)
            table_path = file.name

    print(f"downloaded {filecount} files from {table} ")
    filecount = 0
        
 