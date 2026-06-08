// ========== FUNCTIONS FOR ADMIN PAGES ==========

// Subjects Management
function filterSubjects() {
    alert('Đang lọc danh sách môn học...');
}

function searchSubject() {
    var keyword = document.getElementById('searchKeyword').value;
    alert('Tìm kiếm môn học: ' + keyword);
}

function addSubject() {
    var code = document.getElementById('subjectCode').value;
    var name = document.getElementById('subjectName').value;
    var credits = document.getElementById('subjectCredits').value;
    var department = document.getElementById('subjectDepartment').value;

    if (!code || !name) {
        alert('Vui lòng nhập mã môn và tên môn học!');
        return;
    }

    alert('Đã thêm môn học: ' + name);
    $('#addSubjectModal').modal('hide');
}

function editSubject(id) {

    document.getElementById("editSubjectId").value = id;

    var modal = new bootstrap.Modal(
        document.getElementById("editSubjectModal")
    );

    modal.show();
}

function deleteSubject(id, name) {
    if (confirm('Bạn có chắc muốn xóa môn học "' + name + '"?')) {
        alert('Đã xóa môn học: ' + name);
    }
}

// Questions Management
function filterQuestions() {
    alert('Đang lọc câu hỏi...');
}

function searchQuestions() {
    var keyword = document.getElementById('searchKeyword').value;
    alert('Tìm kiếm câu hỏi: ' + keyword);
}

function toggleSelectAll() {
    var all = document.getElementById('selectAll').checked;
    document.querySelectorAll('.questionCheckbox').forEach(cb => cb.checked = all);
    updateSelectedCount();
}

function updateSelectedCount() {
    var count = document.querySelectorAll('.questionCheckbox:checked').length;
    document.getElementById('selectedCount').innerHTML = '✅ Đã chọn ' + count + ' câu hỏi';
}

function bulkDelete() {
    var count = document.querySelectorAll('.questionCheckbox:checked').length;
    if (count > 0 && confirm('Bạn có chắc muốn xóa ' + count + ' câu hỏi đã chọn?')) {
        alert('Đã xóa ' + count + ' câu hỏi!');
    }
}

function submitAddQuestion() {
    var content = document.getElementById('questionContent').value;
    if (!content) {
        alert('Vui lòng nhập nội dung câu hỏi!');
        return;
    }
    alert('Đã thêm câu hỏi thành công!');
    $('#addQuestionModal').modal('hide');
}

function editQuestion(id) {
    alert('Chỉnh sửa câu hỏi ID: ' + id);
}

function deleteQuestion(id) {
    if (confirm('Bạn có chắc muốn xóa câu hỏi này?')) {
        alert('Đã xóa câu hỏi ID: ' + id);
    }
}

function viewQuestion(id) {
    alert('Xem chi tiết câu hỏi ID: ' + id);
}

function importExcel() {
    alert('Import dữ liệu từ Excel! Vui lòng chọn file.');
}

function exportExcel() {
    alert('Export danh sách câu hỏi ra Excel!');
}

// Exams Management
function filterExams() {
    alert('Đang lọc danh sách đề thi...');
}

function viewExam(id) {
    alert('Xem chi tiết đề thi ID: ' + id);
}

function editExam(id) {
    alert('Chỉnh sửa đề thi ID: ' + id);
}

function copyExam(id) {
    alert('Sao chép đề thi ID: ' + id);
}

function deleteExam(id, name) {
    if (confirm('Bạn có chắc muốn xóa đề thi "' + name + '"?')) {
        alert('Đã xóa đề thi: ' + name);
    }
}

// Settings Management
function saveSettings() {
    alert('Đã lưu cấu hình hệ thống!');
}

function testEmail() {
    alert('Đã gửi email test! Vui lòng kiểm tra hộp thư.');
}

function resetSettings() {
    if (confirm('Bạn có chắc muốn khôi phục cài đặt mặc định?')) {
        alert('Đã khôi phục cài đặt mặc định!');
        location.reload();
    }
}

// Statistics
function exportExcelReport() {
    alert('Xuất báo cáo Excel!');
}

function exportPDFReport() {
    alert('Xuất báo cáo PDF!');
}

// ========== FUNCTIONS FOR TEACHER PAGES ==========

// Teacher Question Bank
function teacherFilterQuestions() {
    alert('Đang lọc câu hỏi...');
}

function teacherSearchQuestions() {
    var keyword = document.getElementById('searchKeyword').value;
    alert('Tìm kiếm câu hỏi: ' + keyword);
}

function teacherAddQuestion() {
    var content = document.getElementById('questionContent').value;
    if (!content) {
        alert('Vui lòng nhập nội dung câu hỏi!');
        return;
    }
    alert('Đã thêm câu hỏi thành công!');
    $('#addQuestionModal').modal('hide');
}

function teacherEditQuestion(id) {
    alert('Chỉnh sửa câu hỏi ID: ' + id);
}

function teacherDeleteQuestion(id) {
    if (confirm('Bạn có chắc muốn xóa câu hỏi này?')) {
        alert('Đã xóa câu hỏi ID: ' + id);
    }
}

// Teacher Exams
function teacherFilterExams() {
    alert('Đang lọc danh sách đề thi...');
}

function teacherViewExam(id) {
    alert('Xem chi tiết đề thi ID: ' + id);
}

function teacherEditExam(id) {
    alert('Chỉnh sửa đề thi ID: ' + id);
}

function teacherCopyExam(id) {
    alert('Sao chép đề thi ID: ' + id);
}

function teacherDeleteExam(id, name) {
    if (confirm('Bạn có chắc muốn xóa đề thi "' + name + '"?')) {
        alert('Đã xóa đề thi: ' + name);
    }
}

function submitCreateExam() {
    var title = document.getElementById('examTitle').value;
    if (!title) {
        alert('Vui lòng nhập tên đề thi!');
        return;
    }
    alert('Đã tạo đề thi "' + title + '" thành công!');
    window.location.href = '/Teacher/Exams';
}

// Teacher Grading
function filterSubmissions() {
    alert('Đang lọc bài thi cần chấm...');
}

function openGradeModal(id, name) {
    document.getElementById('studentName').innerText = name;
    $('#gradeModal').modal('show');
}

function calculateTotal() {
    var score1 = parseFloat(document.getElementById('score1').value) || 0;
    var score2 = parseFloat(document.getElementById('score2').value) || 0;
    var mcScore = parseFloat(document.getElementById('multipleChoiceScore').value) || 0;
    var total = score1 + score2 + mcScore;
    document.getElementById('totalScore').value = total.toFixed(1);
}

function submitGrade() {
    alert('Đã lưu điểm thành công!');
    $('#gradeModal').modal('hide');
}

// ========== FUNCTIONS FOR STUDENT PAGES ==========

// Student History
function filterHistory() {
    alert('Đang lọc lịch sử thi...');
}

function viewDetail(id, title) {
    document.getElementById('detailModalTitle').innerHTML = 'Chi tiết: ' + title;
    $('#detailModal').modal('show');
}

function printResult() {
    window.print();
}

// Student Ranking
function updateRanking() {
    alert('Đang cập nhật bảng xếp hạng...');
}

// Exam Taking
function confirmSubmit() {
    var unanswered = document.querySelectorAll('input[type="radio"]:not(:checked)').length;
    if (unanswered > 0) {
        return confirm('Bạn còn ' + unanswered + ' câu chưa trả lời. Bạn có chắc muốn nộp bài không?');
    }
    return confirm('Bạn có chắc chắn muốn nộp bài thi?');
}