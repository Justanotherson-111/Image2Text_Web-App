import { useState } from "react";
import { searchDocument, type SearchResult } from "../Search/search";

import Toast from "../UI/Toast";
import { Button } from "../ui/button";

interface Props {
    documentId: string;
}

export default function DocumentSearch({ documentId }: Props) {
    const [query, setQuery] = useState("");
    const [results, setResults] = useState<SearchResult[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSearch = async () => {
        if (!query.trim()) return;

        setLoading(true);
        setError(null);

        try {
            const data = await searchDocument(documentId, query);
            setResults(data);
        } catch (err: any) {
            setError(err.response?.data || "Search failed");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="p-4 rounded-xl bg-gray-900 text-white">
            <h3 className="text-lg font-semibold mb-2">Search OCR Text</h3>

            <div className="flex gap-2">
                <input
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    placeholder="Search keyword..."
                    className="flex-1 px-3 py-2 rounded bg-gray-800 border border-gray-700"
                />
                <Button onClick={handleSearch} disabled={loading}>
                    {loading ? "Searching..." : "Search"}
                </Button>
            </div>

            {error && (
                <Toast toasts={[{ id: "search-error", message: error, type: "error" }]} />
            )}

            {/* Results */}
            <div className="mt-4 space-y-3">
                {results.map((r, idx) => (
                    <div
                        key={idx}
                        className="p-3 rounded-lg border border-gray-700 bg-gray-800"
                    >
                        <div className="text-sm text-gray-400">
                            {r.documentTitle} → {r.sectionTitle}
                        </div>

                        <div className="font-semibold">{r.imageName}</div>

                        <p className="mt-1 text-sm text-gray-200">
                            “{r.snippet}”
                        </p>

                        <div className="mt-2">
                            <a
                                href={r.previewUrl}
                                target="_blank"
                                rel="noreferrer"
                                className="text-blue-400 hover:underline text-sm"
                            >
                                View Image
                            </a>
                        </div>
                    </div>
                ))}

                {!loading && results.length === 0 && query && (
                    <p className="text-sm text-gray-400">No results found.</p>
                )}
            </div>
        </div>
    );
}
