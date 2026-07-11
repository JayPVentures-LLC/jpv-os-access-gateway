import assert from "node:assert/strict";
import { after, before, test } from "node:test";
import {
  assertFails,
  initializeTestEnvironment
} from "@firebase/rules-unit-testing";
import { doc, getDoc, setDoc } from "firebase/firestore";
import {
  getBytes,
  ref,
  uploadBytes
} from "firebase/storage";

let environment;

before(async () => {
  environment = await initializeTestEnvironment({
    projectId: "jpv-nexus-production-502019",
    firestore: { rules: "../../firebase/firestore.rules" },
    storage: { rules: "../../firebase/storage.rules" }
  });
});

after(async () => {
  await environment?.cleanup();
});

test("unauthenticated Firestore reads are denied", async () => {
  const db = environment.unauthenticatedContext().firestore();
  await assertFails(getDoc(doc(db, "nexus/test")));
});

test("authenticated Firestore writes are denied by default", async () => {
  const db = environment.authenticatedContext("operator", {
    jpv_role: "enterprise_operator",
    jpv_lanes: ["enterprise"],
    environment: "production",
    schema_version: 1
  }).firestore();
  await assertFails(setDoc(doc(db, "nexus/test"), { unsafe: true }));
});

test("clients cannot append JPV Ledger decisions", async () => {
  const db = environment.authenticatedContext("admin", {
    jpv_role: "jpv_admin",
    jpv_lanes: ["enterprise"],
    environment: "production",
    schema_version: 1
  }).firestore();
  await assertFails(setDoc(doc(db, "ledger/event-1"), { decision: "allow" }));
});

test("unauthenticated Storage reads are denied", async () => {
  const storage = environment.unauthenticatedContext().storage();
  await assertFails(getBytes(ref(storage, "public/test.txt")));
});

test("authenticated Storage writes are denied by default", async () => {
  const storage = environment.authenticatedContext("operator", {
    jpv_role: "creator_operator",
    jpv_lanes: ["creator"],
    environment: "production",
    schema_version: 1
  }).storage();
  await assertFails(uploadBytes(ref(storage, "creator/test.txt"), new Uint8Array([1])));
});

test("test harness is pinned to the production project identifier", () => {
  assert.equal(environment.projectId, "jpv-nexus-production-502019");
});
