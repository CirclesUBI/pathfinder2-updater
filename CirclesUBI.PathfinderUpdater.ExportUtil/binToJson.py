import json
import struct
from pathlib import Path
from decimal import Decimal

def binary_to_json(binary_file_path, json_file_path):
    with open(binary_file_path, 'rb') as bin_file:
        # Read and parse the users section
        address_count = struct.unpack('>I', bin_file.read(4))[0]
        addresses = [bin_file.read(20).hex() for _ in range(address_count)]

        # Read and parse the organizations section
        organization_count = struct.unpack('>I', bin_file.read(4))[0]
        organizations = ["0x" + addresses[struct.unpack('>I', bin_file.read(4))[0]] for _ in range(organization_count)]

        # Read and parse the trust edges section
        trust_edges_count = struct.unpack('>I', bin_file.read(4))[0]
        trust_edges = []
        for _ in range(trust_edges_count):
            user_address, can_send_to_address, limit = struct.unpack('>IIb', bin_file.read(9))
            trust_edges.append({
                "userAddress": "0x" + addresses[user_address],
                "canSendToAddress": "0x" + addresses[can_send_to_address],
                "limit": limit
            })

        # Read and parse the balances section
        balances_count = struct.unpack('>I', bin_file.read(4))[0]
        balances = []
        for _ in range(balances_count):
            user_address, token_owner_address, balance_length = struct.unpack('>IIb', bin_file.read(9))
            balance = int.from_bytes(bin_file.read(balance_length), byteorder='big', signed=True)
            balances.append({
                "userAddress": "0x" + addresses[user_address],
                "tokenOwnerAddress": "0x" + addresses[token_owner_address],
                "balance": str(balance)
            })

        # Create the JSON object and write it to the file
        data = {
            "organizationCount": organization_count,
            "organizations": organizations,
            "trustEdgesCount": trust_edges_count,
            "trustEdges": trust_edges,
            "balancesCount": balances_count,
            "balances": balances
        }

        with open(json_file_path, 'w') as json_file:
            json.dump(data, json_file, indent=4)


if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Usage: python3 script.py <input_binary_file_path> <output_json_file_path>")
        sys.exit(1)

    binary_file_path = sys.argv[1]
    json_file_path = sys.argv[2]

    binary_to_json(binary_file_path, json_file_path)
