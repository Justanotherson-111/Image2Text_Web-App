import api from "@/api/axios";

export interface SearchResult {
  imageId: string;
  imageName: string;
  sectionTitle: string;
  documentTitle: string;
  snippet: string;
  previewUrl: string;
}

export async function searchDocument(
  documentId: string,
  query: string
): Promise<SearchResult[]> {
  const { data } = await api.get(
    `/search/document/${documentId}`,
    { params: { q: query } }
  );

  return data.results;
}
