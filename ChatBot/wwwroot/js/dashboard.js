const apiBase = window.location.origin

const endpoints = {
  subjects: `${apiBase}/api/Subject`,
  documents: `${apiBase}/api/Document`,
  upload: `${apiBase}/api/Document/upload`,
}

const ui = {
  apiBase: document.getElementById('apiBase'),
  subjectsEndpoint: document.getElementById('subjectsEndpoint'),
  documentsEndpoint: document.getElementById('documentsEndpoint'),
  uploadEndpoint: document.getElementById('uploadEndpoint'),
  subjectSelect: document.getElementById('subjectSelect'),
  subjectFilter: document.getElementById('subjectFilter'),
  uploadForm: document.getElementById('uploadForm'),
  uploadBtn: document.getElementById('uploadBtn'),
  fileName: document.getElementById('fileName'),
  successNotice: document.getElementById('successNotice'),
  errorNotice: document.getElementById('errorNotice'),
  loadingRow: document.getElementById('loadingRow'),
  emptyRow: document.getElementById('emptyRow'),
  documentsBody: document.getElementById('documentsBody'),
  refreshDocs: document.getElementById('refreshDocs'),
  reloadDocs: document.getElementById('reloadDocs'),
}

const showNotice = (element, message) => {
  if (!element) return
  element.textContent = message
  element.classList.remove('hidden')
}

const hideNotice = (element) => {
  if (!element) return
  element.classList.add('hidden')
  element.textContent = ''
}

const formatBytes = (value) => {
  if (value === null || value === undefined) return '-'
  const units = ['B', 'KB', 'MB', 'GB']
  let size = Number(value)
  let unitIndex = 0
  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024
    unitIndex += 1
  }
  const digits = size >= 10 || unitIndex === 0 ? 0 : 1
  return `${size.toFixed(digits)} ${units[unitIndex]}`
}

const setLoading = (isLoading) => {
  if (ui.loadingRow) ui.loadingRow.classList.toggle('hidden', !isLoading)
}

const setEmpty = (isEmpty) => {
  if (ui.emptyRow) ui.emptyRow.classList.toggle('hidden', !isEmpty)
}

const clearDocuments = () => {
  if (ui.documentsBody) ui.documentsBody.innerHTML = ''
}

const renderDocuments = (documents) => {
  clearDocuments()
  if (!ui.documentsBody) return
  documents.forEach((doc) => {
    const row = document.createElement('div')
    row.className = 'table-row'
    row.innerHTML = `
      <div>
        <strong>${doc.fileName || '-'}</strong>
        <span class="sub">${doc.uploadDate ? new Date(doc.uploadDate).toLocaleString() : '-'}</span>
      </div>
      <span>${doc.subjectName || '-'}</span>
      <span>${doc.chapterName || 'Default'}</span>
      <span>${formatBytes(doc.fileSize)}</span>
      <span class="status ${doc.indexStatus ? doc.indexStatus.toLowerCase() : ''}">${doc.indexStatus || 'Unknown'}</span>
      <button type="button" data-id="${doc.id}">Reindex</button>
    `
    row.querySelector('button').addEventListener('click', () => reindexDocument(doc.id))
    ui.documentsBody.appendChild(row)
  })
}

const updateStats = (documents, subjects) => {
  const pendingCount = documents.filter((doc) => (doc.indexStatus || '').toLowerCase() === 'pending').length
  if (document.getElementById('statDocuments')) {
    document.getElementById('statDocuments').textContent = documents.length
  }
  if (document.getElementById('statSubjects')) {
    document.getElementById('statSubjects').textContent = subjects.length
  }
  if (document.getElementById('statPending')) {
    document.getElementById('statPending').textContent = pendingCount
  }
}

const loadSubjects = async () => {
  try {
    const response = await fetch(endpoints.subjects)
    if (!response.ok) throw new Error('Khong lay duoc danh sach mon hoc.')
    const data = await response.json()
    const list = Array.isArray(data) ? data : []
    list.forEach((subject) => {
      const option = document.createElement('option')
      option.value = subject
      option.textContent = subject
      ui.subjectSelect.appendChild(option)
      ui.subjectFilter.appendChild(option.cloneNode(true))
    })
    updateStats([], list)
  } catch (error) {
    showNotice(ui.errorNotice, error.message)
  }
}

const loadDocuments = async () => {
  hideNotice(ui.errorNotice)
  setLoading(true)
  setEmpty(false)
  clearDocuments()
  try {
    const url = new URL(endpoints.documents)
    const selected = ui.subjectFilter?.value
    if (selected) url.searchParams.set('subjectName', selected)
    const response = await fetch(url)
    if (!response.ok) throw new Error('Khong lay duoc danh sach tai lieu.')
    const data = await response.json()
    const list = Array.isArray(data) ? data : []
    if (list.length === 0) {
      setEmpty(true)
    } else {
      renderDocuments(list)
    }
    const subjects = Array.from(ui.subjectFilter?.options || [])
      .map((option) => option.value)
      .filter((value) => value)
    updateStats(list, subjects)
  } catch (error) {
    showNotice(ui.errorNotice, error.message)
  } finally {
    setLoading(false)
  }
}

const reindexDocument = async (id) => {
  hideNotice(ui.errorNotice)
  hideNotice(ui.successNotice)
  try {
    const response = await fetch(`${apiBase}/api/Document/${id}/reindex`, { method: 'POST' })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || 'Reindex that bai.')
    }
    const text = await response.text()
    showNotice(ui.successNotice, text || 'Reindex thanh cong.')
    loadDocuments()
  } catch (error) {
    showNotice(ui.errorNotice, error.message)
  }
}

const handleUpload = async (event) => {
  event.preventDefault()
  hideNotice(ui.errorNotice)
  hideNotice(ui.successNotice)

  const formData = new FormData(ui.uploadForm)
  const file = ui.uploadForm.querySelector('input[type="file"]').files[0]
  const subject = formData.get('subjectName')

  if (!file) {
    showNotice(ui.errorNotice, 'Vui long chon file truoc khi upload.')
    return
  }

  if (!subject) {
    showNotice(ui.errorNotice, 'Vui long chon mon hoc.')
    return
  }

  ui.uploadBtn.disabled = true
  ui.uploadBtn.textContent = 'Dang tai len...'

  try {
    const response = await fetch(endpoints.upload, {
      method: 'POST',
      body: formData,
    })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || 'Upload that bai.')
    }
    const data = await response.json()
    showNotice(ui.successNotice, data?.message || 'Tai len thanh cong.')
    ui.uploadForm.reset()
    ui.fileName.textContent = 'Chua chon file'
    loadDocuments()
  } catch (error) {
    showNotice(ui.errorNotice, error.message)
  } finally {
    ui.uploadBtn.disabled = false
    ui.uploadBtn.textContent = 'Upload va index'
  }
}

const init = () => {
  if (ui.apiBase) ui.apiBase.textContent = apiBase
  if (ui.subjectsEndpoint) ui.subjectsEndpoint.textContent = endpoints.subjects
  if (ui.documentsEndpoint) ui.documentsEndpoint.textContent = endpoints.documents
  if (ui.uploadEndpoint) ui.uploadEndpoint.textContent = endpoints.upload

  if (ui.uploadForm) ui.uploadForm.addEventListener('submit', handleUpload)
  if (ui.refreshDocs) ui.refreshDocs.addEventListener('click', loadDocuments)
  if (ui.reloadDocs) ui.reloadDocs.addEventListener('click', loadDocuments)
  if (ui.subjectFilter) ui.subjectFilter.addEventListener('change', loadDocuments)

  const fileInput = ui.uploadForm?.querySelector('input[type="file"]')
  if (fileInput) {
    fileInput.addEventListener('change', () => {
      ui.fileName.textContent = fileInput.files[0]?.name || 'Chua chon file'
    })
  }

  loadSubjects()
  loadDocuments()
}

document.addEventListener('DOMContentLoaded', init)
