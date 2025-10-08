export type UserId = string;

export interface ProjectMember {
  userId: UserId;
  name: string;
  email: string;
  role: "Owner" | "Admin" | "Member" | "Reader";
  removedAt: string | null;
}