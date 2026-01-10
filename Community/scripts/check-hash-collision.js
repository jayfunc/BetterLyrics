// Community/scripts/check-hash-collision.js
const fs = require('fs');
const path = require('path');

const registryPath = path.join(__dirname, '../plugins-registry.json');
const registry = JSON.parse(fs.readFileSync(registryPath, 'utf8'));

function getStableId(pluginId) {
    let hash = 23;
    for (let i = 0; i < pluginId.length; i++) {
        hash = (hash * 31 + pluginId.charCodeAt(i)) | 0;
    }
    return Math.abs(hash) + 1000;
}

const seenIds = new Set();
let hasError = false;

console.log("🔍 Starting to check for plugin ID conflicts...");

registry.forEach(item => {
    const stableId = getStableId(item.id);

    console.log(`Checking [${item.id}] -> Hash: ${stableId}`);

    if (seenIds.has(stableId)) {
        console.error(`⛔ Fatel error! Conflict detected!`);
        console.error(`The hash value (${stableId}) calculated from the plugin ID [${item.id}] is duplicated with an existing plugin.`);
        hasError = true;
    }
    seenIds.add(stableId);
});

if (hasError) {
    console.log("⛔ Check failed, please change the plugin ID.");
    process.exit(1);
} else {
    console.log("✅ The check passed; no conflicts were found.");
    process.exit(0);
}
