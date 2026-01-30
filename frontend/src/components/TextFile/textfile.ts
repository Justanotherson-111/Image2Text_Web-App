import api from "@/api/axios";

export async function getTextFileContent(id: string) {
  const res = await api.get(`/textfile/${id}/content`);
  return res.data as { content: string; isManuallyEdited: boolean };
}

export async function updateTextFileContent(
  id: string,
  content: string
) {
  await api.put(`/textfile/${id}/content`, { content });
}
